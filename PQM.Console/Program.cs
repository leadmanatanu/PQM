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
using Gurux.DLMS.Objects;


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

// Execute DLMS Reading in a continuous loop until stopped by the user
Console.WriteLine("\nStarting continuous DLMS reading loop.");
Console.WriteLine("Press ESC or 'q' to stop reading and exit...");

bool keepRunning = true;
while (keepRunning)
{
    ReadDLMSData(deviceService, deviceParamService, deviceLogService, errorLogPath, logEnabled, lstDevices, clientAddress, serverAddress, authentication, dlmsPassword, useLogicalNameReferencing, standard);

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Cycle completed. Waiting 30 seconds before next run...");
    Console.WriteLine("Press ESC or 'q' to disconnect and exit.");
    Console.WriteLine("==================================================");

    // Sleep for 30 seconds, checking for exit key press periodically
    int sleepIntervalMs = 30000;
    int checkIntervalMs = 100;
    int slept = 0;
    while (slept < sleepIntervalMs)
    {
        try
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    keepRunning = false;
                    break;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Standard input is redirected, fall back to simple sleep
            Thread.Sleep(sleepIntervalMs - slept);
            break;
        }
        Thread.Sleep(checkIntervalMs);
        slept += checkIntervalMs;
    }
}

Console.WriteLine("Disconnected successfully. Exiting program.");



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

                // Ensure ConnectedHeader table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ConnectedHeader' AND xtype='U')
                        CREATE TABLE [ConnectedHeader] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure ConnectedHeader table exists: {ex.Message}");
                }

                // Ensure DLMSObject table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DLMSObject' AND xtype='U')
                        CREATE TABLE [DLMSObject] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [HeaderId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NOT NULL,
                            [ObisCode] NVARCHAR(MAX) NOT NULL,
                            [ObjectType] NVARCHAR(MAX) NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure DLMSObject table exists: {ex.Message}");
                }

                // Ensure ObjectParameter table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ObjectParameter' AND xtype='U')
                        CREATE TABLE [ObjectParameter] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [ObjectId] INT NOT NULL,
                            [AttributeId] INT NOT NULL,
                            [Name] NVARCHAR(MAX) NOT NULL,
                            [DataType] NVARCHAR(MAX) NULL,
                            [AccessType] NVARCHAR(MAX) NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure ObjectParameter table exists: {ex.Message}");
                }

                // Ensure ParameterValue table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ParameterValue' AND xtype='U')
                        CREATE TABLE [ParameterValue] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [ParameterId] INT NOT NULL,
                            [Value] NVARCHAR(MAX) NULL,
                            [Timestamp] DATETIME2 NOT NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure ParameterValue table exists: {ex.Message}");
                }

                // Ensure ProfileGenericEntry table exists
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProfileGenericEntry' AND xtype='U')
                        CREATE TABLE [ProfileGenericEntry] (
                            [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                            [DeviceId] INT NOT NULL,
                            [ObisCode] NVARCHAR(MAX) NOT NULL,
                            [ProfileName] NVARCHAR(MAX) NOT NULL,
                            [EntryTime] DATETIME2 NOT NULL,
                            [ColumnName] NVARCHAR(MAX) NOT NULL,
                            [NumericValue] FLOAT NULL,
                            [TextValue] NVARCHAR(MAX) NULL,
                            [Unit] NVARCHAR(MAX) NULL
                        )
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure ProfileGenericEntry table exists: {ex.Message}");
                }

                // Ensure columns exist on Parameter table
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Parameter') AND name = N'Scaler')
                            ALTER TABLE [Parameter] ADD [Scaler] INT NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Parameter') AND name = N'Unit')
                            ALTER TABLE [Parameter] ADD [Unit] NVARCHAR(MAX) NULL;
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure columns on Parameter exist: {ex.Message}");
                }

                // Ensure columns exist on Register table
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Register') AND name = N'NumericValue')
                            ALTER TABLE [Register] ADD [NumericValue] FLOAT NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Register') AND name = N'ObisCode')
                            ALTER TABLE [Register] ADD [ObisCode] NVARCHAR(MAX) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Register') AND name = N'Scaler')
                            ALTER TABLE [Register] ADD [Scaler] INT NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Register') AND name = N'Unit')
                            ALTER TABLE [Register] ADD [Unit] NVARCHAR(MAX) NULL;
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure columns on Register exist: {ex.Message}");
                }

                // Ensure columns exist on DeviceLog table
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'DeviceLog') AND name = N'NumericValue')
                            ALTER TABLE [DeviceLog] ADD [NumericValue] FLOAT NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'DeviceLog') AND name = N'Unit')
                            ALTER TABLE [DeviceLog] ADD [Unit] NVARCHAR(MAX) NULL;
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not ensure columns on DeviceLog exist: {ex.Message}");
                }

                // Ensure columns exist on ParameterValue table
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ParameterValue') AND name = N'Value' AND is_nullable = 0)
                            ALTER TABLE [ParameterValue] ALTER COLUMN [Value] NVARCHAR(MAX) NULL;
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DLMS Reader] Warning: Could not alter column on ParameterValue: {ex.Message}");
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

                    // Ensure ConnectedHeader exists for the device
                    ConnectedHeader? header = dbContext.ConnectedHeader.FirstOrDefault(h => h.DeviceId == item.Id);
                    if (header == null)
                    {
                        header = new ConnectedHeader
                        {
                            DeviceId = item.Id,
                            Name = $"{item.Name} Header"
                        };
                        dbContext.ConnectedHeader.Add(header);
                        dbContext.SaveChanges();
                    }

                    // Ensure all parameters exist in database in one batch
                    bool parametersAdded = false;
                    foreach (var obj in reader.Objects)
                    {
                        var dbParam = existingParams.FirstOrDefault(p => p.ObisCode == obj.LogicalName);
                        if (dbParam == null)
                        {
                            converter.UpdateOBISCodeInformation(obj);
                            string paramName = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obj.LogicalName}" : obj.Description;
                            
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
                            existingParams.Add(dbParam); // Add to cache list
                            parametersAdded = true;
                        }
                    }
                    if (parametersAdded)
                    {
                        dbContext.SaveChanges();
                    }

                    // Map parameters to device in one batch
                    bool mappingsAdded = false;
                    foreach (var obj in reader.Objects)
                    {
                        var dbParam = existingParams.FirstOrDefault(p => p.ObisCode == obj.LogicalName);
                        if (dbParam != null)
                        {
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
                                existingMappings.Add(mapping);
                                mappingsAdded = true;
                            }
                        }
                    }
                    if (mappingsAdded)
                    {
                        dbContext.SaveChanges();
                    }

                    foreach (var obj in reader.Objects)
                    {
                        // Resolve description
                        converter.UpdateOBISCodeInformation(obj);
                        string paramName = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obj.LogicalName}" : obj.Description;
                        var dbParam = existingParams.FirstOrDefault(p => p.ObisCode == obj.LogicalName);

                        // Read values
                        string attr2Val = "";
                        try
                        {
                            if (obj.ObjectType == ObjectType.ProfileGeneric)
                            {
                                var lastTs = GetLastProfileTimestamp(dbContext, item.Id, obj.LogicalName, out var _);
                                attr2Val = reader.ReadObjectValue(obj, lastTs);
                            }
                            else
                            {
                                attr2Val = reader.ReadObjectValue(obj);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[DLMS Reader] Read failed for {paramName} ({obj.LogicalName}): {ex.Message}");
                            Console.ResetColor();
                            attr2Val = $"Error: {ex.Message}";

                             // Check if the connection is dead/socket lost
                             bool isDisconnected = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                                                  ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                                                  ex.Message.Contains("socket", StringComparison.OrdinalIgnoreCase) ||
                                                  ex.Message.Contains("disconnected", StringComparison.OrdinalIgnoreCase) ||
                                                  ex.InnerException is System.Net.Sockets.SocketException ||
                                                  ex.InnerException is System.IO.IOException;

                            if (isDisconnected)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine("[DLMS Reader] Connection lost or device offline. Aborting scan loop for this device to disconnect immediately and continue.");
                                Console.ResetColor();
                                break; // exit parameter scan loop early
                            }
                        }
                        string attr3Val = "";
                        int? scaler = null;
                        string? unit = null;
                        double? numericValue = null;

                        if (obj is GXDLMSRegister reg)
                        {
                            scaler = (int)reg.Scaler;
                            unit = reg.Unit.ToString();
                            attr3Val = $"{{{scaler}, {unit}}}";

                            if (dbParam.Scaler != scaler || dbParam.Unit != unit || dbParam.Attribute3 != attr3Val)
                            {
                                dbParam.Scaler = scaler;
                                dbParam.Unit = unit;
                                dbParam.Attribute3 = attr3Val;
                                dbContext.SaveChanges();
                            }

                            if (!string.IsNullOrEmpty(attr2Val) && !attr2Val.StartsWith("Error"))
                            {
                                if (double.TryParse(attr2Val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVal))
                                {
                                    numericValue = parsedVal * Math.Pow(10, reg.Scaler);
                                }
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
                                DateStamp = dateStamp,
                                NumericValue = numericValue,
                                Unit = unit
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
                                        NumericValue = numericValue,
                                        Scaler = scaler,
                                        Unit = unit,
                                        ObisCode = obj.LogicalName,
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
                                            var existing = dbContext.IecHdlcSetup
                                                .FirstOrDefault(x => x.DeviceId == item.Id && x.Name == attr.Key);
                                            if (existing != null)
                                            {
                                                existing.Value = val;
                                                existing.DateEntered = dateStamp;
                                                existing.ObjectType = obj.ObjectType.ToString();
                                            }
                                            else
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
                                            var existing = dbContext.TcpUdpSetup
                                                .FirstOrDefault(x => x.DeviceId == item.Id && x.Name == attr.Key);
                                            if (existing != null)
                                            {
                                                existing.Value = val;
                                                existing.DateEntered = dateStamp;
                                                existing.ObjectType = obj.ObjectType.ToString();
                                            }
                                            else
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
                                            var existing = dbContext.Ip4Setup
                                                .FirstOrDefault(x => x.DeviceId == item.Id && x.Name == attr.Key);
                                            if (existing != null)
                                            {
                                                existing.Value = val;
                                                existing.DateEntered = dateStamp;
                                                existing.ObjectType = obj.ObjectType.ToString();
                                            }
                                            else
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
                                        var existing = dbContext.MacAddressSetup
                                            .FirstOrDefault(x => x.DeviceId == item.Id && x.Name == "MAC Address");
                                        if (existing != null)
                                        {
                                            existing.Value = val;
                                            existing.DateEntered = dateStamp;
                                            existing.ObjectType = obj.ObjectType.ToString();
                                        }
                                        else
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
                                        }
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
                                            var existing = dbContext.AssociationLogicalName
                                                .FirstOrDefault(x => x.DeviceId == item.Id && x.Name == attr.Key);
                                            if (existing != null)
                                            {
                                                existing.Value = val;
                                                existing.DateEntered = dateStamp;
                                                existing.ObjectType = obj.ObjectType.ToString();
                                            }
                                            else
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
                            else if (obj.ObjectType == ObjectType.ProfileGeneric)
                            {
                                try
                                {
                                    var pgRecord = dbContext.ProfileGeneric
                                        .FirstOrDefault(p => p.DeviceId == item.Id && (p.Name == obj.LogicalName || p.ObjectType == obj.LogicalName));
                                    
                                    string mergedJson;
                                    if (pgRecord != null)
                                    {
                                        mergedJson = MergeProfileGenericJson(pgRecord.Value, attr2Val);
                                        pgRecord.Value = mergedJson;
                                        pgRecord.DateEntered = dateStamp;
                                        dbContext.ProfileGeneric.Update(pgRecord);
                                    }
                                    else
                                    {
                                        mergedJson = attr2Val;
                                        var newRecord = new PQM.Core.Entities.ProfileGeneric
                                        {
                                            DeviceId = item.Id,
                                            Name = paramName,
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = mergedJson,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.ProfileGeneric.Add(newRecord);
                                    }
                                    dbContext.SaveChanges();

                                    // Parse and flatten the profile entries into ProfileGenericEntry
                                    if (!string.IsNullOrEmpty(attr2Val) && !attr2Val.StartsWith("Error"))
                                    {
                                        try
                                        {
                                            var records = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(attr2Val);
                                            if (records != null && records.Count > 0)
                                            {
                                                int addedCount = 0;
                                                foreach (var row in records)
                                                {
                                                    DateTime? entryTime = null;
                                                    if (row.TryGetValue("Clock", out var clockStr) && !string.IsNullOrEmpty(clockStr))
                                                    {
                                                        entryTime = ParseDlmsClock(clockStr);
                                                    }

                                                    if (!entryTime.HasValue)
                                                    {
                                                        entryTime = dateStamp;
                                                    }

                                                    foreach (var kvp in row)
                                                    {
                                                        if (kvp.Key == "Clock") continue;

                                                        string columnName = kvp.Key;
                                                        string columnValue = kvp.Value;

                                                        var obis = obj.LogicalName;
                                                        var existingEntry = dbContext.ProfileGenericEntry
                                                            .FirstOrDefault(e => e.DeviceId == item.Id 
                                                                && e.ObisCode == obis 
                                                                && e.EntryTime == entryTime.Value 
                                                                && e.ColumnName == columnName);

                                                        if (existingEntry == null)
                                                        {
                                                            double? numVal = null;
                                                            string? textVal = null;
                                                            if (double.TryParse(columnValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedDouble))
                                                            {
                                                                numVal = parsedDouble;
                                                            }
                                                            else
                                                            {
                                                                textVal = columnValue;
                                                            }

                                                            string? unitName = dbContext.Parameter
                                                                .FirstOrDefault(p => p.Name == columnName)?.Unit;

                                                            var newEntry = new ProfileGenericEntry
                                                            {
                                                                DeviceId = item.Id,
                                                                ObisCode = obis,
                                                                ProfileName = paramName,
                                                                EntryTime = entryTime.Value,
                                                                ColumnName = columnName,
                                                                NumericValue = numVal,
                                                                TextValue = textVal,
                                                                Unit = unitName
                                                            };
                                                            dbContext.ProfileGenericEntry.Add(newEntry);
                                                            addedCount++;
                                                        }
                                                    }
                                                }
                                                if (addedCount > 0)
                                                {
                                                    dbContext.SaveChanges();
                                                    Console.WriteLine($"[DLMS Reader] Flattened and saved {addedCount} new ProfileGeneric entries for {paramName}.");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[DLMS Reader] Failed to flatten ProfileGeneric: {ex.Message}");
                                        }
                                    }

                                    // Parse event profiles and write them to the EventLog table if applicable
                                    bool isEventProfile = obj.LogicalName.StartsWith("0.0.99.98") || paramName.ToLower().Contains("event");
                                    if (isEventProfile && !string.IsNullOrEmpty(attr2Val) && !attr2Val.StartsWith("Error"))
                                    {
                                        try
                                        {
                                            var records = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(attr2Val);
                                            if (records != null && records.Count > 0)
                                            {
                                                int eventAdded = 0;
                                                foreach (var row in records)
                                                {
                                                    DateTime? eventTime = null;
                                                    if (row.TryGetValue("Clock", out var clockStr) && !string.IsNullOrEmpty(clockStr))
                                                    {
                                                        eventTime = ParseDlmsClock(clockStr);
                                                    }

                                                    if (!eventTime.HasValue)
                                                    {
                                                        eventTime = dateStamp;
                                                    }

                                                    string eventTypeStr = "DLMS Event";
                                                    if (row.TryGetValue("Event Code", out var code)) eventTypeStr = $"Event {code}";
                                                    else if (row.TryGetValue("Event ID", out var idVal)) eventTypeStr = $"Event {idVal}";
                                                    else if (row.TryGetValue("Event", out var ev)) eventTypeStr = ev;

                                                    var existingEvent = dbContext.EventLog
                                                        .FirstOrDefault(e => e.DeviceId == item.Id 
                                                            && e.Start_Time == eventTime.Value 
                                                            && e.EventType == eventTypeStr);

                                                    if (existingEvent == null)
                                                    {
                                                        string? phase = null;
                                                        if (row.TryGetValue("Phase", out var ph)) phase = ph;

                                                        double? duration = null;
                                                        if (row.TryGetValue("Duration", out var durStr) && double.TryParse(durStr, out double dur)) duration = dur;

                                                        var newEvent = new EventLog
                                                        {
                                                            DeviceId = item.Id,
                                                            EventType = eventTypeStr,
                                                            Start_Time = eventTime.Value,
                                                            CreatedDate = dateStamp,
                                                            Phase = phase,
                                                            Duration = duration,
                                                            Date = eventTime.Value
                                                        };
                                                        dbContext.EventLog.Add(newEvent);
                                                        eventAdded++;
                                                    }
                                                }
                                                if (eventAdded > 0)
                                                {
                                                    dbContext.SaveChanges();
                                                    Console.WriteLine($"[DLMS Reader] Extracted and saved {eventAdded} event logs from {paramName} profile.");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[DLMS Reader] Failed to extract events from profile: {ex.Message}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save ProfileGeneric to database: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.Clock)
                            {
                                try
                                {
                                    var existing = dbContext.Clock
                                        .FirstOrDefault(c => c.DeviceId == item.Id && c.Name == paramName);
                                    if (existing != null)
                                    {
                                        existing.Value = attr2Val;
                                        existing.DateEntered = dateStamp;
                                        existing.ObjectType = obj.ObjectType.ToString();
                                    }
                                    else
                                    {
                                        var clockVal = new PQM.Core.Entities.Clock
                                        {
                                            DeviceId = item.Id,
                                            Name = paramName,
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = attr2Val,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.Clock.Add(clockVal);
                                    }
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save to Clock table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.ScriptTable)
                            {
                                try
                                {
                                    var existing = dbContext.ScriptTable
                                        .FirstOrDefault(s => s.DeviceId == item.Id && s.Name == paramName);
                                    if (existing != null)
                                    {
                                        existing.Value = attr2Val;
                                        existing.DateEntered = dateStamp;
                                        existing.ObjectType = obj.ObjectType.ToString();
                                    }
                                    else
                                    {
                                        var scriptVal = new PQM.Core.Entities.ScriptTable
                                        {
                                            DeviceId = item.Id,
                                            Name = paramName,
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = attr2Val,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.ScriptTable.Add(scriptVal);
                                    }
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save to ScriptTable table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.ActionSchedule)
                            {
                                try
                                {
                                    var existing = dbContext.ActionSchedule
                                        .FirstOrDefault(a => a.DeviceId == item.Id && a.Name == paramName);
                                    if (existing != null)
                                    {
                                        existing.Value = attr2Val;
                                        existing.DateEntered = dateStamp;
                                        existing.ObjectType = obj.ObjectType.ToString();
                                    }
                                    else
                                    {
                                        var actionVal = new PQM.Core.Entities.ActionSchedule
                                        {
                                            DeviceId = item.Id,
                                            Name = paramName,
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = attr2Val,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.ActionSchedule.Add(actionVal);
                                    }
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save to ActionSchedule table: {ex.Message}");
                                }
                            }
                            else if (obj.ObjectType == ObjectType.ActivityCalendar)
                            {
                                try
                                {
                                    var existing = dbContext.ActivityCalendar
                                        .FirstOrDefault(a => a.DeviceId == item.Id && a.Name == paramName);
                                    if (existing != null)
                                    {
                                        existing.Value = attr2Val;
                                        existing.DateEntered = dateStamp;
                                        existing.ObjectType = obj.ObjectType.ToString();
                                    }
                                    else
                                    {
                                        var activityVal = new PQM.Core.Entities.ActivityCalendar
                                        {
                                            DeviceId = item.Id,
                                            Name = paramName,
                                            ObjectType = obj.ObjectType.ToString(),
                                            Value = attr2Val,
                                            DateEntered = dateStamp
                                        };
                                        dbContext.ActivityCalendar.Add(activityVal);
                                    }
                                    dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DLMS Reader] Failed to save to ActivityCalendar table: {ex.Message}");
                                }
                            }

                            // Sync with DLMSObject / ObjectParameter / ParameterValue chain
                            try
                            {
                                var dlmsObj = dbContext.DLMSObject.FirstOrDefault(o => o.HeaderId == header.Id && o.ObisCode == obj.LogicalName);
                                if (dlmsObj == null)
                                {
                                    dlmsObj = new DLMSObject
                                    {
                                        HeaderId = header.Id,
                                        Name = paramName,
                                        ObisCode = obj.LogicalName,
                                        ObjectType = obj.ObjectType.ToString()
                                    };
                                    dbContext.DLMSObject.Add(dlmsObj);
                                    dbContext.SaveChanges();
                                }

                                var param2 = dbContext.ObjectParameter.FirstOrDefault(p => p.ObjectId == dlmsObj.Id && p.AttributeId == 2);
                                if (param2 == null)
                                {
                                    param2 = new ObjectParameter
                                    {
                                        ObjectId = dlmsObj.Id,
                                        AttributeId = 2,
                                        Name = "Value",
                                        DataType = "String",
                                        AccessType = "Read"
                                    };
                                    dbContext.ObjectParameter.Add(param2);
                                    dbContext.SaveChanges();
                                }

                                var val2 = new ParameterValue
                                {
                                    ParameterId = param2.Id,
                                    Value = attr2Val,
                                    Timestamp = dateStamp
                                };
                                dbContext.ParameterValue.Add(val2);

                                if (obj is GXDLMSRegister)
                                {
                                    var param3 = dbContext.ObjectParameter.FirstOrDefault(p => p.ObjectId == dlmsObj.Id && p.AttributeId == 3);
                                    if (param3 == null)
                                    {
                                        param3 = new ObjectParameter
                                        {
                                            ObjectId = dlmsObj.Id,
                                            AttributeId = 3,
                                            Name = "Scaler/Unit",
                                            DataType = "String",
                                            AccessType = "Read"
                                        };
                                        dbContext.ObjectParameter.Add(param3);
                                        dbContext.SaveChanges();
                                    }

                                    if (!string.IsNullOrEmpty(attr3Val))
                                    {
                                        var val3 = new ParameterValue
                                        {
                                            ParameterId = param3.Id,
                                            Value = attr3Val,
                                            Timestamp = dateStamp
                                        };
                                        dbContext.ParameterValue.Add(val3);
                                    }
                                }
                                dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DLMS Reader] Failed to save to DLMSObject/ParameterValue chain: {ex.Message}");
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

static DateTime? ParseDlmsClock(string octetString)
{
    if (string.IsNullOrWhiteSpace(octetString)) return null;
    string[] parts = octetString.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 7) return null;
    try
    {
        int year = Convert.ToInt32(parts[0] + parts[1], 16);
        int month = Convert.ToInt32(parts[2], 16);
        int day = Convert.ToInt32(parts[3], 16);
        int hour = Convert.ToInt32(parts[5], 16);
        int minute = Convert.ToInt32(parts[6], 16);
        int second = parts.Length > 7 && parts[7] != "FF" ? Convert.ToInt32(parts[7], 16) : 0;
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }
    catch
    {
        return null;
    }
}

static DateTime? GetLastProfileTimestamp(PQM.Infrastructure.DataContext dbContext, int deviceId, string obisCode, out string? existingJson)
{
    existingJson = null;
    try
    {
        var pgRecord = dbContext.ProfileGeneric
            .FirstOrDefault(p => p.DeviceId == deviceId && (p.Name == obisCode || p.ObjectType == obisCode));
        
        if (pgRecord != null && !string.IsNullOrEmpty(pgRecord.Value))
        {
            existingJson = pgRecord.Value;
            var records = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(pgRecord.Value);
            if (records != null && records.Count > 0)
            {
                for (int i = records.Count - 1; i >= 0; i--)
                {
                    if (records[i].TryGetValue("Clock", out var clockStr) && !string.IsNullOrEmpty(clockStr))
                    {
                        var dt = ParseDlmsClock(clockStr);
                        if (dt.HasValue) return dt;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DLMS Reader] Failed to parse last profile timestamp: {ex.Message}");
    }
    return null;
}

static string MergeProfileGenericJson(string? existingJson, string newJson)
{
    if (string.IsNullOrEmpty(existingJson) || existingJson == "[]") return newJson;
    if (string.IsNullOrEmpty(newJson) || newJson == "[]" || newJson.StartsWith("Error")) return existingJson;

    try
    {
        var existingRecords = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(existingJson) ?? new List<Dictionary<string, string>>();
        var newRecords = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(newJson) ?? new List<Dictionary<string, string>>();

        var existingClocks = new HashSet<string>();
        foreach (var rec in existingRecords)
        {
            if (rec.TryGetValue("Clock", out var clock))
            {
                existingClocks.Add(clock);
            }
        }

        foreach (var rec in newRecords)
        {
            if (rec.TryGetValue("Clock", out var clock))
            {
                if (!existingClocks.Contains(clock))
                {
                    existingRecords.Add(rec);
                }
            }
            else
            {
                existingRecords.Add(rec);
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(existingRecords);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DLMS Reader] Failed to merge ProfileGeneric JSON: {ex.Message}");
        return newJson;
    }
}

