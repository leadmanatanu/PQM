using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PQM.Server.Models;
using PQM.Core.IRepositories;
using PQM.Core.Entities;
using System.Net;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FTPController : ControllerBase
    {
        private readonly IFTPSettingService _ftpSettingService;
        private readonly ILogger<FTPController> _logger;

        public FTPController(
            ILogger<FTPController> logger,
            IFTPSettingService ftpSettingService)
        {
            _logger = logger;
            _ftpSettingService = ftpSettingService;
        }

        // -------------------- GET FTP SETTINGS --------------------
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

        // -------------------- ADD / UPDATE FTP --------------------
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

        // -------------------- FTP CONNECTION TEST --------------------
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