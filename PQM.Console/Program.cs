// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using PQM.Infrastructure.Services;
using PQM.Console;
using PQM.Core.DomainServices;
using PQM.Core.Entities;
using PQM.Core.Helper;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Microsoft.EntityFrameworkCore;


Console.WriteLine("Start reading ftp files");


var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

var host = CreateHostBuilder(args, config.GetSection("ConnectionString").Value ?? string.Empty).Build();

static IHostBuilder CreateHostBuilder(string[] args, string strDbConnection) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        services.AddScoped<ICSVService, CSVService>()
        .AddScoped<ISFTPService, SFTPService>()
        .AddScoped<IDeviceService>(s => new DeviceService(strDbConnection))
        .AddScoped<IDeviceParameterService>(s => new DeviceParameterService(strDbConnection))
        .AddScoped<IDeviceLogService>(s => new DeviceLogService(strDbConnection))
        .AddScoped<IFTPSettingService>(s => new FTPSettingService(strDbConnection)));


// Get the service from DI
var csvService = host.Services.GetRequiredService<ICSVService>();
var ftpService = host.Services.GetRequiredService<ISFTPService>();
var deviceService = host.Services.GetRequiredService<IDeviceService>();
var deviceParamService = host.Services.GetRequiredService<IDeviceParameterService>();
var deviceLogService = host.Services.GetRequiredService<IDeviceLogService>();
var ftpSettingService = host.Services.GetRequiredService<IFTPSettingService>();


//string url = config["FtpSetting:URL"];
//string user = config["FtpSetting:User"];
//string password = config["FtpSetting:Password"];
string localFolder = config["FtpSetting:LocalFolder"] ?? string.Empty;
string errorLogPath = config["ErrorLog:Path"] ?? string.Empty;
bool logEnabled = Convert.ToBoolean(config["ErrorLog:LogEnabled"]);

if (!Directory.Exists(errorLogPath))
{
    errorLogPath = string.Empty;// Save logs in program files
}

var ftpSetting = ftpSettingService.GetFTPSetting();
string url = ftpSetting != null && !string.IsNullOrEmpty(ftpSetting.FtpHost) 
    ? $"{ftpSetting.FtpHost.TrimEnd('/')}/{ftpSetting.RootFolderName?.Trim('/')}/" 
    : string.Empty;
string user = ftpSetting?.UserName ?? string.Empty;
string password = ftpSetting?.Password ?? string.Empty;

// FTP validations are bypassed as we are only performing DLMS meter readings
/*
if (String.IsNullOrEmpty(url))
{
    ErrorLog.LogErrorMessage("FTP URL is missing", errorLogPath);
    return;
}
if (String.IsNullOrEmpty(user))
{
    ErrorLog.LogErrorMessage("FTP User is missing", errorLogPath);
    return;
}
if (String.IsNullOrEmpty(password))
{
    ErrorLog.LogErrorMessage("FTP Password is missing", errorLogPath);
    return;
}
if (String.IsNullOrEmpty(localFolder) || !Directory.Exists(localFolder))
{
    ErrorLog.LogErrorMessage("CSV Local Folder Location is missing", errorLogPath);
    return;
}
*/

var lstDevices = deviceService.GetDevices().ToList();

int clientAddress = config.GetValue<int>("DlmsSettings:ClientAddress", 1);
int serverAddress = config.GetValue<int>("DlmsSettings:ServerAddress", 1);
string authStr = config.GetValue<string>("DlmsSettings:Authentication", "None");
string dlmsPassword = config.GetValue<string>("DlmsSettings:Password", "");
bool useLogicalNameReferencing = config.GetValue<bool>("DlmsSettings:UseLogicalNameReferencing", true);
string standardStr = config.GetValue<string>("DlmsSettings:Standard", "DLMS");
Enum.TryParse<Authentication>(authStr, true, out var authentication);
Enum.TryParse<Standard>(standardStr, true, out var standard);

// Only execute DLMS Reading, as requested
ReadDLMSData(deviceService, deviceParamService, deviceLogService, errorLogPath, logEnabled, lstDevices, clientAddress, serverAddress, authentication, dlmsPassword, useLogicalNameReferencing, standard);



static void ReadDLMSData(IDeviceService? deviceService, IDeviceParameterService? deviceParamService, IDeviceLogService? deviceLogService, string errorLogPath, bool logEnabled, List<Device> lstDevices, int clientAddress, int serverAddress, Authentication authentication, string password, bool useLogicalNameReferencing, Standard standard)
{
    if (deviceService == null || deviceParamService == null || deviceLogService == null)
        return;

    Console.WriteLine("==================================================");
    Console.WriteLine("Start reading DLMS smart meters...");
    Console.WriteLine("==================================================");

    foreach (var item in lstDevices)
    {
        if (string.IsNullOrEmpty(item.IP) || item.PORT == 0)
        {
            Console.WriteLine($"Skipping device {item.Name} due to missing IP or Port.");
            continue;
        }

        try
        {
            Console.WriteLine($"\n[DLMS Reader] Connecting to device: {item.Name} at {item.IP}:{item.PORT}...");
            if (logEnabled)
                ErrorLog.LogErrorMessage("Connecting to DLMS meter " + item.Name + " at " + item.IP + ":" + item.PORT, errorLogPath);

            using (var dbContext = new PQM.Infrastructure.DataContext(((DeviceService)deviceService)._connectionString))
            {
                // Ensure Register table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Register' AND xtype='U')
                        CREATE TABLE [Register] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure Register table exists: {ex.Message}");
                }

                // Ensure Data table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Data' AND xtype='U')
                        CREATE TABLE [Data] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure Data table exists: {ex.Message}");
                }

                // Ensure IecHdlcSetup table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='IecHdlcSetup' AND xtype='U')
                        CREATE TABLE [IecHdlcSetup] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure IecHdlcSetup table exists: {ex.Message}");
                }

                // Ensure TcpUdpSetup table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TcpUdpSetup' AND xtype='U')
                        CREATE TABLE [TcpUdpSetup] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure TcpUdpSetup table exists: {ex.Message}");
                }

                // Ensure Ip4Setup table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Ip4Setup' AND xtype='U')
                        CREATE TABLE [Ip4Setup] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure Ip4Setup table exists: {ex.Message}");
                }

                // Ensure MacAddressSetup table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MacAddressSetup' AND xtype='U')
                        CREATE TABLE [MacAddressSetup] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure MacAddressSetup table exists: {ex.Message}");
                }

                // Ensure AssociationLogicalName table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AssociationLogicalName' AND xtype='U')
                        CREATE TABLE [AssociationLogicalName] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL,
                            [ObjectType] NVARCHAR(MAX) NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [DateEntered] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure AssociationLogicalName table exists: {ex.Message}");
                }

                using (var reader = new DLMSReader(item.IP, item.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard))
                {
                    reader.Connect();
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[DLMS Reader] Connected successfully to {item.Name}!");
                    Console.ResetColor();

                    // 1. Fetch Association View (Get all objects from the device)
                    Console.WriteLine("[DLMS Reader] Retrieving Association View from meter...");
                    var associationView = reader.GetAssociationView(); 

                    // Update OBIS definitions via converter
                    var converter = new GXDLMSConverter();
                    converter.UpdateOBISCodeInformation(reader.Objects);

                    // 2. Synchronize (equalize) the parameters in the database
                    Console.WriteLine("[DLMS Reader] Synchronizing meter parameters with local database...");
                    
                    var existingParams = dbContext.Parameter.ToList();
                    var existingMappings = dbContext.DeviceParameterMapping.Where(m => m.DeviceId == item.Id).ToList();
                    
                    var logsToSave = new List<DeviceLog>();
                    DateTime dateStamp = DateTime.UtcNow;

                    // Read Meter Clock if possible
                    try
                    {
                        string clockVal = reader.ReadRegister("0.0.1.0.0.255", "Clock - 0.0.1.0.0.255");
                        if (!string.IsNullOrEmpty(clockVal) && !clockVal.StartsWith("Error"))
                        {
                            if (DateTime.TryParse(clockVal, out var parsedDate))
                            {
                                dateStamp = parsedDate;
                                Console.WriteLine($"[DLMS Reader] Meter Date/Time: {dateStamp}");
                            }
                        }
                    }
                    catch { }

                    Console.WriteLine("\nScanning parameters (show parameters and their values)...");
                    Console.WriteLine(new string('-', 120));
                    Console.WriteLine($"{"Name / Description",-45} | {"Object Type",-12} | {"OBIS Code",-15} | {"Attribute 2",-18} | {"Attribute 3"}");
                    Console.WriteLine(new string('-', 120));

                    foreach (var obj in reader.Objects)
                    {
                        // Process ONLY Register, ExtendedRegister, DemandRegister, Data, IecHdlcSetup, TcpUdpSetup, Ip4Setup, MacAddressSetup, AssociationLogicalName
                        if (obj.ObjectType != ObjectType.Register && 
                            obj.ObjectType != ObjectType.ExtendedRegister && 
                            obj.ObjectType != ObjectType.DemandRegister &&
                            obj.ObjectType != ObjectType.Data &&
                            obj.ObjectType != ObjectType.IecHdlcSetup &&
                            obj.ObjectType != ObjectType.TcpUdpSetup &&
                            obj.ObjectType != ObjectType.Ip4Setup &&
                            obj.ObjectType != ObjectType.MacAddressSetup &&
                            obj.ObjectType != ObjectType.AssociationLogicalName)
                        {
                            continue;
                        }

                        // Resolve description
                        converter.UpdateOBISCodeInformation(obj);
                        string paramName = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obj.LogicalName}" : obj.Description;

                        // Check if Parameter exists in database, if not insert it (Equalise parameter)
                        var dbParam = existingParams.FirstOrDefault(p => p.ObisCode == obj.LogicalName);
                        if (dbParam == null)
                        {
                            dbParam = new PQM.Core.Entities.Parameter
                            {
                                Name = paramName,
                                ObisCode = obj.LogicalName,
                                ObjectType = obj.ObjectType.ToString(),
                                IsActive = true,
                                IsDeleted = false,
                                CreatedDate = DateTime.UtcNow
                            };
                            dbContext.Parameter.Add(dbParam);
                            dbContext.SaveChanges();
                            existingParams.Add(dbParam); // Add to cache list
                        }

                        // Map parameter to device if not mapped
                        var mapping = existingMappings.FirstOrDefault(m => m.ParameterId == dbParam.Id);
                        if (mapping == null)
                        {
                            mapping = new DeviceParameterMapping
                            {
                                DeviceId = item.Id,
                                ParameterId = dbParam.Id,
                                DateStamp = DateTime.UtcNow
                            };
                            dbContext.DeviceParameterMapping.Add(mapping);
                            dbContext.SaveChanges();
                            existingMappings.Add(mapping);
                        }

                        // Read values
                        string attr2Val = reader.ReadObjectValue(obj);
                        string attr3Val = "";

                        if (obj.ObjectType == ObjectType.Register || obj.ObjectType == ObjectType.ExtendedRegister || obj.ObjectType == ObjectType.DemandRegister)
                        {
                            // Read Scaler & Unit (Attribute 3)
                            attr3Val = reader.ReadObjectAttribute3(obj);
                            
                            // Save Attribute 3 (Scaler & Unit) to Parameter table in DB
                            if (dbParam.Attribute3 != attr3Val)
                            {
                                dbParam.Attribute3 = attr3Val;
                                dbContext.SaveChanges();
                            }
                        }

                        // Print to console
                        string formattedVal = attr2Val.StartsWith("Error") ? "[Read Error]" : attr2Val;
                        Console.WriteLine($"{paramName,-45} | {obj.ObjectType,-12} | {obj.LogicalName,-15} | {formattedVal,-18} | {attr3Val}");

                        // Add to save list if read was successful
                        if (!string.IsNullOrEmpty(attr2Val) && !attr2Val.StartsWith("Error"))
                        {
                            logsToSave.Add(new DeviceLog
                            {
                                DeviceId = item.Id,
                                ParameterId = dbParam.Id,
                                Value = attr2Val.Length > 500 ? attr2Val.Substring(0, 500) : attr2Val,
                                DateStamp = dateStamp
                            });

                            if (obj.ObjectType == ObjectType.Register || obj.ObjectType == ObjectType.ExtendedRegister || obj.ObjectType == ObjectType.DemandRegister)
                            {
                                try
                                {
                                    var registerData = new Register
                                    {
                                        DeviceId = item.Id,
                                        Name = paramName,
                                        ObjectType = obj.ObjectType.ToString(),
                                        Value = attr2Val,
                                        DateEntered = dateStamp
                                    };
                                    dbContext.Register.Add(registerData);
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save register to Register table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.Data)
                            {
                                try
                                {
                                    var dataVal = new PQM.Core.Entities.Data
                                    {
                                        DeviceId = item.Id,
                                        Name = paramName,
                                        ObjectType = obj.ObjectType.ToString(),
                                        Value = attr2Val,
                                        DateEntered = dateStamp
                                    };
                                    dbContext.Data.Add(dataVal);
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save register to Data table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.IecHdlcSetup)
                            {
                                var attributes = new Dictionary<string, int>
                                {
                                    { "Speed", 2 },
                                    { "Transmit Window Size", 3 },
                                    { "Receive Window Size", 4 },
                                    { "Transmit Maximum Length", 5 },
                                    { "Receive Maximum Length", 6 },
                                    { "Internal Timeout", 7 },
                                    { "Inactivity Timeout", 8 },
                                    { "Device Address", 9 }
                                };

                                foreach (var attr in attributes)
                                {
                                    try
                                    {
                                        string val = reader.ReadObjectAttribute(obj, attr.Value);
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("Error"))
                                        {
                                            var hdlcVal = new PQM.Core.Entities.IecHdlcSetup
                                            {
                                                DeviceId = item.Id,
                                                Name = attr.Key,
                                                ObjectType = obj.ObjectType.ToString(),
                                                Value = val,
                                                DateEntered = dateStamp
                                            };
                                            dbContext.IecHdlcSetup.Add(hdlcVal);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[DLMS Reader] Failed to save {attr.Key} to IecHdlcSetup table: {ex.Message}");
                                    }
                                }
                                try
                                {
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save changes for IecHdlcSetup: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.TcpUdpSetup)
                            {
                                var attributes = new Dictionary<string, int>
                                {
                                    { "Port", 2 },
                                    { "IP Reference", 3 },
                                    { "Max Segment Size", 4 },
                                    { "Max Connections", 5 },
                                    { "Inactivity Timeout", 6 }
                                };

                                foreach (var attr in attributes)
                                {
                                    try
                                    {
                                        string val = reader.ReadObjectAttribute(obj, attr.Value);
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("Error"))
                                        {
                                            var tcpVal = new PQM.Core.Entities.TcpUdpSetup
                                            {
                                                DeviceId = item.Id,
                                                Name = attr.Key,
                                                ObjectType = obj.ObjectType.ToString(),
                                                Value = val,
                                                DateEntered = dateStamp
                                            };
                                            dbContext.TcpUdpSetup.Add(tcpVal);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[DLMS Reader] Failed to save {attr.Key} to TcpUdpSetup table: {ex.Message}");
                                    }
                                }
                                try
                                {
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save changes for TcpUdpSetup: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.Ip4Setup)
                            {
                                var attributes = new Dictionary<string, int>
                                {
                                    { "Data Link Layer Reference", 2 },
                                    { "IP Address", 3 },
                                    { "Multicast IP Address", 4 },
                                    { "IP Options", 5 },
                                    { "Subnet Mask", 6 },
                                    { "Gateway IP Address", 7 },
                                    { "Use DHCP", 8 },
                                    { "Primary DNS Address", 9 },
                                    { "Secondary DNS Address", 10 }
                                };

                                foreach (var attr in attributes)
                                {
                                    try
                                    {
                                        string val = reader.ReadObjectAttribute(obj, attr.Value);
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("Error"))
                                        {
                                            var ipVal = new PQM.Core.Entities.Ip4Setup
                                            {
                                                DeviceId = item.Id,
                                                Name = attr.Key,
                                                ObjectType = obj.ObjectType.ToString(),
                                                Value = val,
                                                DateEntered = dateStamp
                                            };
                                            dbContext.Ip4Setup.Add(ipVal);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[DLMS Reader] Failed to save {attr.Key} to Ip4Setup table: {ex.Message}");
                                    }
                                }
                                try
                                {
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save changes for Ip4Setup: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.MacAddressSetup)
                            {
                                try
                                {
                                    string val = reader.ReadObjectAttribute(obj, 2);
                                    if (!string.IsNullOrEmpty(val) && !val.StartsWith("Error"))
                                    {
                                        var macVal = new PQM.Core.Entities.MacAddressSetup
                                        {
                                            DeviceId = item.Id,
                                            Name = "MAC Address",
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = val,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.MacAddressSetup.Add(macVal);
                                        dbContext.SaveChanges();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save MAC Address to MacAddressSetup table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.AssociationLogicalName)
                            {
                                var attributes = new Dictionary<string, int>
                                {
                                    { "Object List", 2 },
                                    { "Associated Partners ID", 3 },
                                    { "Application Context Name", 4 },
                                    { "xDLMS Context Info", 5 },
                                    { "Authentication Mechanism Name", 6 },
                                    { "LLS Secret", 7 },
                                    { "Association Status", 8 },
                                    { "Security Setup Reference", 9 },
                                    { "User List", 10 }
                                };

                                foreach (var attr in attributes)
                                {
                                    try
                                    {
                                        string val = reader.ReadObjectAttribute(obj, attr.Value);
                                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("Error"))
                                        {
                                            var assocVal = new PQM.Core.Entities.AssociationLogicalName
                                            {
                                                DeviceId = item.Id,
                                                Name = attr.Key,
                                                ObjectType = obj.ObjectType.ToString(),
                                                Value = val,
                                                DateEntered = dateStamp
                                            };
                                            dbContext.AssociationLogicalName.Add(assocVal);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[DLMS Reader] Failed to save {attr.Key} to AssociationLogicalName table: {ex.Message}");
                                    }
                                }
                                try
                                {
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save changes for AssociationLogicalName: {ex.Message}");
                                }
                            }
                        }
                    }
                    Console.WriteLine(new string('-', 120));

                    // Save read values (Attribute 2) to DeviceLog table
                    if (logsToSave.Count > 0)
                    {
                        var dataSaved = deviceLogService.AddBulkDeviceLogs(logsToSave);
                        if (dataSaved)
                        {
                            deviceService.UpdateLastSync(item.Id, dateStamp);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n[DLMS Reader] Successfully saved {logsToSave.Count} register values to database.");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n[DLMS Reader] Failed to save values to database.");
                            Console.ResetColor();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[DLMS Reader] Error while reading from {item.Name}: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
            ErrorLog.LogErrorMessage("Error while reading DLMS data of " + item.Name + ". Error: " + ex.Message, errorLogPath);
        }
    }
    Console.WriteLine("\n==================================================");
    Console.WriteLine("Finished reading DLMS smart meters.");
    Console.WriteLine("==================================================");
}

