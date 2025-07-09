using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
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
            try
            {
                if (String.IsNullOrEmpty(device.Name))
                {
                    _apiResponse.Status = false;
                    _apiResponse.Data = null;
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
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);

            }

        }
    }
}
