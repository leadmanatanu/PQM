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
    public class ProfileGenericController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly string _connectionString;

        public ProfileGenericController(IConfiguration configuration)
        {
            _apiResponse = new APIResponse();
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet("entries")]
        public ActionResult GetEntries(int deviceId, string obisCode, string? columnName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var dbContext = new DataContext(_connectionString);
                var query = dbContext.ProfileGenericEntry
                    .Where(e => e.DeviceId == deviceId && e.ObisCode == obisCode);

                if (!string.IsNullOrEmpty(columnName))
                {
                    query = query.Where(e => e.ColumnName == columnName);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.EntryTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.EntryTime <= endDate.Value);
                }

                var entries = query.OrderBy(e => e.EntryTime).ToList();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = entries;
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
