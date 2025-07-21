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
        .AddScoped<IDeviceLogService>(s => new DeviceLogService(strDbConnection)));


// Get the service from DI
var csvService = host.Services.GetService<ICSVService>();
var ftpService = host.Services.GetService<ISFTPService>();
var deviceService = host.Services.GetService<IDeviceService>();
var deviceParamService = host.Services.GetService<IDeviceParameterService>();
var deviceLogService = host.Services.GetService<IDeviceLogService>();

string url = config["FtpSetting:URL"];
string user = config["FtpSetting:User"];
string password = config["FtpSetting:Password"];
string localFolder = config["FtpSetting:LocalFolder"];

if (String.IsNullOrEmpty(url))
{
    ErrorLog.LogErrorMessage("FTP URL is missing");
    return;
}
if (String.IsNullOrEmpty(user))
{
    ErrorLog.LogErrorMessage("FTP User is missing");
    return;
}
if (String.IsNullOrEmpty(password))
{
    ErrorLog.LogErrorMessage("FTP Password is missing");
    return;
}
if (String.IsNullOrEmpty(localFolder) || !Directory.Exists(localFolder))
{
    ErrorLog.LogErrorMessage("CSV Local Folder Location is missing");
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
            ErrorLog.LogErrorMessage("No parameter mapping exist for device " + item.Name);
            continue;
        }
        if (String.IsNullOrEmpty(item.FtpFolder))
        {
            ErrorLog.LogErrorMessage("Ftp Folder name is empty for device " + item.Name);
            continue;
        }

        // download files from ftp
        List<string> lstFtpFiles = ftpService.GetFiles(url, user, password, item.FtpFolder, localFolder);
        ErrorLog.LogErrorMessage("Total files downloaded for " + item.Name + " =>" + lstFtpFiles.Count);

        // Read and add files in database
        foreach (string file in lstFtpFiles)
        {
            //Console.WriteLine($"Reading file : {Path.GetFileName(file)}");
            ErrorLog.LogErrorMessage("Reading file of " + item.Name + " =>" + file);
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
                        ErrorLog.LogErrorMessage("Adding logs fails for device " + item.Name + " and file => " + filePath);
                    }
                }
            }
            else
            {
                ErrorLog.LogErrorMessage("File does not exist => " + filePath);
            }
        }
    }
    catch (Exception ex)
    {
        ErrorLog.LogErrorMessage("Error while reading data of " + item.Name + ". Error " + ex.Message);
    }
}