using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;
using System.DirectoryServices.Protocols;
using System.Net;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FTPController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IFTPSettingService _ftpSettingService;
        private readonly ILogger<FTPController> _logger;

        public FTPController(ILogger<FTPController> logger, IFTPSettingService ftpSettingService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _ftpSettingService = ftpSettingService;
        }

        [HttpGet(Name = "GetFTP")]
        public ActionResult Get()
        {
            var data = _ftpSettingService.GetFTPSetting();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpPut(Name = "AddUpdateFTP")]
        public ActionResult Put([FromBody] FTPSetting ftpData)
        {
            if (!ValidateFtpData(ftpData))
            {
                _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                return Ok(_apiResponse);
            }

            var result = _ftpSettingService.AddUpdateFTP(ftpData);
            if (result)
            {
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = ftpData;
            }
            else
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { "FTP not found." };
                return Ok(_apiResponse);
            }
            return Ok(_apiResponse);
        }

        [HttpGet("FTPConnectionTest")]
        public ActionResult FTPConnectionTest([FromQuery] FTPSetting ftpData)
        {
            try
            {
                if (!ValidateFtpData(ftpData))
                {
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpData.FtpHost);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(ftpData.UserName, ftpData.Password);
                _apiResponse.Status = false;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = ftpData;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        private bool ValidateFtpData(FTPSetting ftpData)
        {
            bool isValidData = true;
            _apiResponse.Errors = new List<string> { };
            if (String.IsNullOrEmpty(ftpData.FtpHost))
            {
                _apiResponse.Errors.Add("Ftp Host is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(ftpData.UserName))
            {
                _apiResponse.Errors.Add("User Name is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(ftpData.Password))
            {
                _apiResponse.Errors.Add("Password is required.");
                isValidData = false;
            }
            return isValidData;
        }

    }
}
