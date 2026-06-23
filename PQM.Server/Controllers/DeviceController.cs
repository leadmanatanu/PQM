using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;
using System.DirectoryServices.Protocols;
using Microsoft.EntityFrameworkCore;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceLogController> _logger;

        public DeviceController(ILogger<DeviceLogController> logger, IDeviceService deviceService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _deviceService = deviceService;
        }

        [HttpGet(Name = "GetDevices")]
        public ActionResult Get()
        {
            var data = _deviceService.GetDevices().ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpPost(Name = "AddDevice")]
        public ActionResult Post([FromBody] Device device)
        {
            _apiResponse.Status = false;
            _apiResponse.Data = null;
            try
            {
                _apiResponse.Errors = new List<string> { };
                if (!RequiredFieldValidation(device))
                {
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }
                if (!IsDeviceAlreadyExist(device))
                {
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }

                device.CreatedDate = DateTime.UtcNow;
                var data = _deviceService.AddDevice(device);
                device.Id = data;
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = device;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);

            }
        }


        [HttpPut(Name = "UpdateDevice")]
        public ActionResult Put([FromBody] Device device)
        {
            try
            {
                _apiResponse.Status = false;
                _apiResponse.Data = null;
                if (!RequiredFieldValidation(device))
                {
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }
                if (!IsDeviceAlreadyExist(device))
                {
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }
                var result = _deviceService.UpdateDevice(device);
                if (result)
                {
                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = device;
                }
                else
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string> { "No device found." };
                    return Ok(_apiResponse);
                }
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);

            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _apiResponse.Status = false;
                    _apiResponse.Data = null;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    _apiResponse.Errors = new List<string> { "Id is required." };
                    return Ok(_apiResponse);
                }
                var result = _deviceService.DeleteDevice(id);
                if (result)
                {
                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = null;
                }
                else
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string> { "No device found." };
                    return Ok(_apiResponse);
                }
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);

            }
        }

        private bool RequiredFieldValidation(Device device)
        {
            bool isValidData = true;
            if (String.IsNullOrEmpty(device.Name))
            {
                _apiResponse.Errors.Add("Name is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(device.ConsumerNumber))
            {
                _apiResponse.Errors.Add("Consumer Number is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(device.SerialNumber))
            {
                _apiResponse.Errors.Add("Serial Number is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(device.FtpFolder))
            {
                _apiResponse.Errors.Add("Ftp Folder is required.");
                isValidData = false;
            }
            if (String.IsNullOrEmpty(device.IP))
            {
                _apiResponse.Errors.Add("IP is required.");
                isValidData = false;
            }
            if (device.PORT <= 0)
            {
                _apiResponse.Errors.Add("PORT is required.");
                isValidData = false;
            }
            return isValidData;
        }

        private bool IsDeviceAlreadyExist(Device device)
        {
            bool isValidData = true;
            var lstDevices = _deviceService.GetDevices();
            if (device.Id > 0) // Edit scanario
            {
                lstDevices = _deviceService.GetDevices().Where(x => x.Id != device.Id && (x.Name == device.Name ||
                            x.SerialNumber == device.SerialNumber || x.ConsumerNumber == device.ConsumerNumber ||
                            (x.IP == device.IP && x.PORT == device.PORT) || x.FtpFolder == device.FtpFolder));
            }
            _apiResponse.Errors = new List<string> { };
            if (lstDevices.Any(x => x.Name == device.Name))
            {
                _apiResponse.Errors.Add("Device Name already exist.");
                isValidData = false;
            }
            if (lstDevices.Any(x => x.SerialNumber == device.SerialNumber))
            {
                _apiResponse.Errors.Add("Serial Number already exist.");
                isValidData = false;
            }
            if (lstDevices.Any(x => x.ConsumerNumber == device.ConsumerNumber))
            {
                _apiResponse.Errors.Add("Consumer Number already exist.");
                isValidData = false;
            }
            if (lstDevices.Any(x => x.IP == device.IP && x.PORT == device.PORT))
            {
                _apiResponse.Errors = new List<string> { "IP address and Port already exist." };
                isValidData = false;

            }
            if (lstDevices.Any(x => x.FtpFolder == device.FtpFolder))
            {
                _apiResponse.Errors.Add("FTP folder name already exist.");
                isValidData = false;
            }

            return isValidData;
        }
    }
}
