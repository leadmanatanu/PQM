using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PQM.Server.Models;
using PQM.Core.IRepositories;
using PQM.Core.Entities;
using System.Net;
using Microsoft.Extensions.Configuration;
using PQM.Infrastructure;
using PQM.Core.DomainServices;
using System.IO;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FTPController : ControllerBase
    {
        private readonly IFTPSettingService _ftpSettingService;
        private readonly ILogger<FTPController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public FTPController(
            ILogger<FTPController> logger,
            IFTPSettingService ftpSettingService,
            IConfiguration configuration)
        {
            _logger = logger;
            _ftpSettingService = ftpSettingService;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet]
        public IActionResult Get()
        {
            var response = new APIResponse
            {
                Status = true,
                StatusCode = HttpStatusCode.OK,
                Data = _ftpSettingService.GetFTPSetting()
            };
            return Ok(response);
        }

        [HttpPut]
        public IActionResult Put([FromBody] FTPSetting ftpData)
        {
            var response = new APIResponse();

            if (!ValidateFtpData(ftpData, response))
            {
                response.StatusCode = HttpStatusCode.NotAcceptable;
                return Ok(response);
            }

            if (_ftpSettingService.AddUpdateFTP(ftpData))
            {
                response.Status = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Data = ftpData;
            }
            else
            {
                response.Status = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Errors.Add("FTP not found.");
            }

            return Ok(response);
        }

        [HttpGet("FTPConnectionTest")]
        public IActionResult FTPConnectionTest([FromQuery] FTPSetting ftpData)
        {
            var response = new APIResponse();

            if (!ValidateFtpData(ftpData, response))
            {
                response.StatusCode = HttpStatusCode.NotAcceptable;
                return Ok(response);
            }

            try
            {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                var request = (FtpWebRequest)WebRequest.Create(new Uri(ftpData.FtpHost));
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(
                    ftpData.UserName,
                    ftpData.Password
                );
                request.EnableSsl = false;
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                using var responseFtp = (FtpWebResponse)request.GetResponse();

                response.Status = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Data = "FTP connection successful";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection failed");

                response.Status = false;
                response.StatusCode = HttpStatusCode.NotAcceptable;
                response.Errors.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("ImportLocalCSV")]
        public IActionResult ImportLocalCSVFiles([FromQuery] int deviceId)
        {
            var response = new APIResponse();
            try
            {
                // Navigate to PQM.Server/CSVFiles
                var csvFolder = Path.Combine(Directory.GetCurrentDirectory(), "CSVFiles");
                if (!Directory.Exists(csvFolder))
                {
                    response.Status = false;
                    response.Errors.Add($"CSVFiles directory not found at: {csvFolder}");
                    return Ok(response);
                }

                var files = Directory.GetFiles(csvFolder, "*.csv");
                if (files.Length == 0)
                {
                    response.Status = true;
                    response.Data = "No CSV files found in CSVFiles folder to import.";
                    return Ok(response);
                }

                using var db = new DataContext(_connectionString);
                var device = db.Device.FirstOrDefault(d => d.Id == deviceId);
                if (device == null)
                {
                    response.Status = false;
                    response.Errors.Add($"Device with ID {deviceId} not found.");
                    return Ok(response);
                }

                // Get string representations of all active parameter IDs for lookup mapping
                var mappedParams = db.Parameter.Select(p => p.Id.ToString()).ToList();

                var importedLogsCount = 0;
                var importedEventsCount = 0;
                var csvService = new CSVService();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    
                    if (fileName.Contains("event", StringComparison.OrdinalIgnoreCase))
                    {
                        var eventLogs = csvService.ReadEventLog(deviceId, "GeneralEvent", file);
                        if (eventLogs != null && eventLogs.Any())
                        {
                            db.EventLog.AddRange(eventLogs);
                            importedEventsCount += eventLogs.Count;
                        }
                    }
                    else
                    {
                        var deviceLogs = csvService.ReadCSVData(deviceId, file, mappedParams);
                        if (deviceLogs != null && deviceLogs.Any())
                        {
                            db.DeviceLog.AddRange(deviceLogs);
                            importedLogsCount += deviceLogs.Count;
                        }
                    }
                }

                db.SaveChanges();

                response.Status = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Data = $"Successfully imported {importedLogsCount} device readings and {importedEventsCount} event readings from CSVFiles folder.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing local CSV files");
                response.Status = false;
                response.Errors.Add(ex.Message);
            }
            return Ok(response);
        }

        // -------------------- VALIDATION --------------------
        private bool ValidateFtpData(FTPSetting ftpData, APIResponse response)
        {
            if (string.IsNullOrWhiteSpace(ftpData.FtpHost))
                response.Errors.Add("FTP Host is required.");

            if (string.IsNullOrWhiteSpace(ftpData.UserName))
                response.Errors.Add("User Name is required.");

            if (string.IsNullOrWhiteSpace(ftpData.Password))
                response.Errors.Add("Password is required.");

            return !response.Errors.Any();
        }
    }
}