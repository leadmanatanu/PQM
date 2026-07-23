using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParameterController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ParameterController> _logger;
        private readonly string _connectionString;

        public ParameterController(ILogger<ParameterController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get([FromQuery] int? deviceId)
        {
            using var db = new DataContext(_connectionString);
            var query = db.Parameter.Where(p => p.IsActive && !p.IsDeleted);

            if (deviceId.HasValue)
            {
                var device = db.Device.FirstOrDefault(d => d.Id == deviceId.Value && !d.IsDeleted);
                if (device != null)
                {
                    var deviceType = string.IsNullOrEmpty(device.TypeName) ? "ABT" : device.TypeName;
                    query = query.Where(p => p.TypeName == deviceType);
                }
            }

            var data = query.ToList();
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
                using var db = new DataContext(_connectionString);
                var data = db.Parameter.FirstOrDefault(p => p.Id == id && p.IsActive && !p.IsDeleted);
                if (data == null)
                {
                    return NotFound(new { error = "Parameter not found." });
                }
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
