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
    public class ActivityCalendarController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly string _connectionString;

        public ActivityCalendarController(IConfiguration configuration)
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
                var latestCalendar = dbContext.ActivityCalendar
                    .Where(a => a.DeviceId == deviceId)
                    .OrderByDescending(a => a.DateEntered)
                    .FirstOrDefault();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = latestCalendar;
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
