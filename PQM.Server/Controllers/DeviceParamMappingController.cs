using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;
using System.DirectoryServices.Protocols;
using PQM.Infrastructure.Repositories;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceParamMappingController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IDeviceParameterService _deviceParamMappingService;
        private readonly ILogger<DeviceLogController> _logger;

        public DeviceParamMappingController(ILogger<DeviceLogController> logger, IDeviceParameterService deviceParamMappingService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _deviceParamMappingService = deviceParamMappingService;
        }

        [HttpPost("{id}")]
        public ActionResult Post(int id)
        {
            var result = _deviceParamMappingService.GetDeviceParameterMapping(id);
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = result;
            return Ok(_apiResponse);
        }

        [HttpPost(Name = "DeviceParamMapping")]
        public ActionResult Post([FromBody] List<DeviceParameterMapping> data)
        {
            var result = _deviceParamMappingService.AddDeviceParameterMapping(data);
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = result;
            return Ok(_apiResponse);
        }
    }
}
