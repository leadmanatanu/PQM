using Microsoft.AspNetCore.Mvc;
using PQM.Server.Models;
using PQM.Infrastructure;
using PQM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClockController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly string _connectionString;

        public ClockController(IConfiguration configuration)
        {
            _apiResponse = new APIResponse();
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet("latest")]
        public ActionResult GetLatest(int deviceId)
        {
            try
            {
                using var dbContext = new DataContext(_connectionString);
                var latestClock = dbContext.Clock
                    .Where(c => c.DeviceId == deviceId)
                    .OrderByDescending(c => c.DateEntered)
                    .FirstOrDefault();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = latestClock;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }
    }
}
