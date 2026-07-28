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
            try
            {
                using var db = new DataContext(_connectionString);
                var data = db.Parameter
                    .Where(p => p.IsVisible)
                    .Select(p => new
                    {
                        p.Id,
                        p.ProfileId,
                        p.Name,
                        p.ObisCode,
                        p.Description,
                        p.Unit,
                        p.DataType,
                        p.ObjectType,
                        p.AttributeIndex,
                        p.IsHistorical,
                        p.IsVisible,
                        p.Scaler
                    })
                    .ToList();

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
