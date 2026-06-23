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
    public class ParameterController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IParameterService _parameterService;
        private readonly ILogger<DeviceLogController> _logger;

        public ParameterController(ILogger<DeviceLogController> logger, IParameterService parameterService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _parameterService = parameterService;
        }

        [HttpGet(Name = "GetParameters")]
        public ActionResult Get()
        {
            var data = _parameterService.GetParameters().ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpGet("{id}")]
        public ActionResult Get(int id)
        {
            try
            {
                var data = _parameterService.GetParameters(id).ToList();
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
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
