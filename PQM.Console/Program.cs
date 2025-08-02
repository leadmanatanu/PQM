// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PQM.Console;
using PQM.Core.DomainServices;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;


Console.WriteLine("Start reading ftp files");

var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

var host = CreateHostBuilder(args, config.GetSection("ConnectionString").Value).Build();

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
var csvService = host.Services.GetService<ICSVService>();
var ftpService = host.Services.GetService<ISFTPService>();
var deviceService = host.Services.GetService<IDeviceService>();
var deviceParamService = host.Services.GetService<IDeviceParameterService>();
var deviceLogService = host.Services.GetService<IDeviceLogService>();
var ftpSettingService = host.Services.GetService<IFTPSettingService>();


//string url = config["FtpSetting:URL"];
//string user = config["FtpSetting:User"];
//string password = config["FtpSetting:Password"];
string localFolder = config["FtpSetting:LocalFolder"];
string errorLogPath = config["ErrorLog:Path"];
bool logEnabled = Convert.ToBoolean(config["ErrorLog:LogEnabled"]);

if (!Directory.Exists(errorLogPath))
{
    errorLogPath = string.Empty;// Save logs in program files
}

var ftpSetting = ftpSettingService.GetFTPSetting();
string url = Path.Combine(ftpSetting.FtpHost, ftpSetting.RootFolderName);
string user = ftpSetting.UserName;
string password = ftpSetting.Password;

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

var lstDevices = deviceService.GetDevices().ToList();
foreach (var item in lstDevices)
{
    try
    {
        var mappedParatmeter = deviceParamService.GetDeviceParameterMapping(item.Id).Select(x => x.ParameterId.ToString()).ToList();
        if (mappedParatmeter.Count <= 0) // TODO discuss => do we need to download files if parameter mapping does not exist for meter
        {
            if (logEnabled)
                ErrorLog.LogErrorMessage("No parameter mapping exist for device " + item.Name, errorLogPath);
            continue;
        }
        if (String.IsNullOrEmpty(item.FtpFolder))
        {
            ErrorLog.LogErrorMessage("Ftp Folder name is empty for device " + item.Name, errorLogPath);
            continue;
        }

        // download files from ftp
        List<string> lstFtpFiles = ftpService.GetFiles(url, user, password, item.FtpFolder, localFolder);
        if (logEnabled)
            ErrorLog.LogErrorMessage("Total files downloaded for " + item.Name + " =>" + lstFtpFiles.Count, errorLogPath);

        // Read and add files in database
        foreach (string file in lstFtpFiles)
        {
            if (logEnabled)
                ErrorLog.LogErrorMessage("Reading file of " + item.Name + " =>" + file, errorLogPath);
            string filePath = localFolder + file;
            if (System.IO.File.Exists(filePath))
            {
                List<DeviceLog> lstDeviceLogs = csvService.ReadCSVData(item.Id, filePath, mappedParatmeter);
                if (lstDeviceLogs.Count > 0)
                {
                    var data = deviceLogService.AddBulkDeviceLogs(lstDeviceLogs);
                    if (data)
                    {
                        // update last date in device table
                        deviceService.UpdateLastSync(item.Id, lstDeviceLogs.LastOrDefault().DateStamp);
                    }
                    else
                    {
                        ErrorLog.LogErrorMessage("Adding logs fails for device " + item.Name + " and file => " + filePath, errorLogPath);
                    }
                }
            }
            else
            {
                ErrorLog.LogErrorMessage("File does not exist => " + filePath, errorLogPath);
            }
        }
    }
    catch (Exception ex)
    {
        ErrorLog.LogErrorMessage("Error while reading data of " + item.Name + ". Error " + ex.Message, errorLogPath);
    }
}