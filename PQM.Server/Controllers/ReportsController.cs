using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ReportsController> _logger;
        private readonly string _connectionString;

        public ReportsController(ILogger<ReportsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpPost("generate")]
        public IActionResult GenerateReport([FromBody] ReportRequest request)
        {
            _apiResponse.Errors.Clear();
            if (request == null || request.DeviceId <= 0 || request.ParameterIds == null || request.ParameterIds.Count == 0)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors.Add("Invalid request parameters. Device ID and at least one Parameter ID must be specified.");
                return Ok(_apiResponse);
            }

            try
            {
                using var dbContext = new DataContext(_connectionString);

                // Fetch parameter details to map IDs to Names and Units
                var parameters = dbContext.Parameter
                    .Where(p => request.ParameterIds.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => new { p.Name, p.Unit });

                // Ensure dates are parsed properly
                var startDate = request.StartDate;
                var endDate = request.EndDate.Date.AddDays(1).AddTicks(-1); // include the full end day

                // Query logs
                var logs = dbContext.DeviceLog
                    .Where(l => l.DeviceId == request.DeviceId && 
                                request.ParameterIds.Contains(l.ParameterId) && 
                                l.DateStamp >= startDate && 
                                l.DateStamp <= endDate)
                    .ToList();

                // Group logs by exact DateStamp timestamp
                var groupedLogs = logs.GroupBy(l => l.DateStamp)
                    .Select(g => {
                        var values = new Dictionary<string, string>();
                        foreach (var log in g)
                        {
                            if (parameters.TryGetValue(log.ParameterId, out var param))
                            {
                                values[param.Name] = log.Value;
                            }
                        }

                        return new ReportRow
                        {
                            Timestamp = g.Key,
                            Values = values
                        };
                    })
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                var result = new ReportResult
                {
                    Columns = parameters.Values.Select(p => p.Name).ToList(),
                    Rows = groupedLogs
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report for device {DeviceId}", request.DeviceId);
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Errors.Add(ex.Message);
                return Ok(_apiResponse);
            }
        }
    }

    public class ReportRequest
    {
        public int DeviceId { get; set; }
        public List<int> ParameterIds { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ReportRow
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();
    }

    public class ReportResult
    {
        public List<string> Columns { get; set; } = new();
        public List<ReportRow> Rows { get; set; } = new();
    }
}
