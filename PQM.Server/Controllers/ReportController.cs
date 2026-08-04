using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class ReportController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ReportController> _logger;
        private readonly string _connectionString;

        public ReportController(ILogger<ReportController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet("aggregate")]
        public IActionResult GetAggregatedReport([FromQuery] ReportSearchParams searchParams)
        {
            try
            {
                if (searchParams.DeviceId <= 0)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Errors = new List<string> { "DeviceId is required." };
                    return Ok(_apiResponse);
                }

                int interval = searchParams.IntervalMinutes > 0 ? searchParams.IntervalMinutes : 15;
                int pageNumber = searchParams.PageNumber > 0 ? searchParams.PageNumber : 1;
                int pageSize = searchParams.PageSize > 0 ? searchParams.PageSize : 20;

                var (totalTimestamps, results) = ExecuteAggregation(searchParams, interval, pageNumber, pageSize);

                var result = new ParameterValueSearchResult
                {
                    DeviceLogSearch = results,
                    TotalCount = totalTimestamps
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing aggregated report query");
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpGet("export")]
        public IActionResult ExportAggregatedReport([FromQuery] ReportSearchParams searchParams)
        {
            try
            {
                if (searchParams.DeviceId <= 0)
                {
                    return BadRequest(new { status = false, message = "DeviceId is required." });
                }

                int interval = searchParams.IntervalMinutes > 0 ? searchParams.IntervalMinutes : 15;
                var (_, readings) = ExecuteAggregation(searchParams, interval, pageNumber: 1, pageSize: int.MaxValue);

                var timestamps = readings
                    .Where(r => r.DateStamp.HasValue)
                    .Select(r => r.DateStamp!.Value)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                var paramsMap = new Dictionary<int, string>();
                foreach (var r in readings)
                {
                    if (!paramsMap.ContainsKey(r.ParameterId))
                        paramsMap[r.ParameterId] = r.ParameterName;
                }

                var cellLookup = new Dictionary<int, Dictionary<DateTime, string>>();
                foreach (var r in readings)
                {
                    if (!r.DateStamp.HasValue) continue;
                    if (!cellLookup.TryGetValue(r.ParameterId, out var dict))
                    {
                        dict = new Dictionary<DateTime, string>();
                        cellLookup[r.ParameterId] = dict;
                    }
                    dict[r.DateStamp.Value] = r.Value;
                }

                var sb = new StringBuilder();

                // Report Header Metadata
                sb.AppendLine($"\"Report Type\",\"Interval Aggregated Report ({interval} min buckets)\"");
                sb.AppendLine($"\"Generated At\",\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
                sb.AppendLine();

                // Table Header
                sb.Append("\"Parameter\"");
                foreach (var ts in timestamps)
                {
                    sb.Append($",\"{ts:yyyy-MM-dd HH:mm:ss}\"");
                }
                sb.AppendLine();

                // Data Rows
                foreach (var kvp in paramsMap)
                {
                    int pId = kvp.Key;
                    string pName = kvp.Value;

                    sb.Append($"\"{pName.Replace("\"", "\"\"")}\"");
                    foreach (var ts in timestamps)
                    {
                        string val = cellLookup.TryGetValue(pId, out var dict) && dict.TryGetValue(ts, out var v) ? v : "";
                        sb.Append($",\"{val.Replace("\"", "\"\"")}\"");
                    }
                    sb.AppendLine();
                }

                string fileName = $"AggregatedReport_{interval}min_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

                return File(bytes, "application/vnd.ms-excel", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting aggregated report");
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        private (int TotalTimestamps, List<ParameterValueSearch> Results) ExecuteAggregation(
            ReportSearchParams searchParams, int intervalMinutes, int pageNumber, int pageSize)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            DateTime startDate = searchParams.StartDate != default ? searchParams.StartDate : DateTime.UtcNow.AddDays(-1);
            DateTime endDate = searchParams.EndDate != default ? searchParams.EndDate : DateTime.UtcNow;

            string paramIdsCsv = "";
            if (searchParams.ParameterIds != null && searchParams.ParameterIds.Count > 0)
            {
                paramIdsCsv = string.Join(",", searchParams.ParameterIds.Where(id => id > 0));
            }

            string sql = @"
                WITH ScaledReadings AS (
                    SELECT 
                        p.Id AS ParameterId,
                        p.Name AS ParameterName,
                        p.ObjectType,
                        p.AggregationType,
                        CASE 
                            WHEN p.Scaler IS NOT NULL AND p.Scaler <> 0 
                                THEN rv.ValueNumeric * POWER(10E0, CAST(p.Scaler AS FLOAT))
                            ELSE rv.ValueNumeric
                        END AS ScaledValueNumeric,
                        DATEADD(minute, 
                            (DATEDIFF(minute, '2000-01-01', rs.EntryTimestampUtc) / @IntervalMinutes) * @IntervalMinutes, 
                            '2000-01-01') AS BucketTimestamp
                    FROM ReadingValues rv
                    INNER JOIN Parameters p ON rv.ParameterId = p.Id
                    INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
                    WHERE rs.DeviceId = @DeviceId
                      AND (@ProfileId IS NULL OR p.ProfileId = @ProfileId)
                      AND (@ObjectType IS NULL OR p.ObjectType = @ObjectType)
                      AND rs.EntryTimestampUtc >= @StartDate
                      AND rs.EntryTimestampUtc <= @EndDate
                      AND rv.ValueNumeric IS NOT NULL
                ),
                AggregatedBuckets AS (
                    SELECT 
                        ParameterId,
                        ParameterName,
                        BucketTimestamp,
                        CASE 
                            WHEN AggregationType = 'Max' OR ParameterName LIKE 'Cum%' OR ParameterName LIKE 'Cumulative%' 
                                THEN CAST(ROUND(MAX(ScaledValueNumeric), 2) AS VARCHAR(50))
                            ELSE CAST(ROUND(AVG(ScaledValueNumeric), 2) AS VARCHAR(50))
                        END AS AggregatedValue
                    FROM ScaledReadings
                    GROUP BY ParameterId, ParameterName, AggregationType, BucketTimestamp
                )
                SELECT 
                    ParameterId,
                    ParameterName,
                    BucketTimestamp AS DateStamp,
                    AggregatedValue AS Value
                FROM AggregatedBuckets
                ORDER BY ParameterId, BucketTimestamp;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceId", searchParams.DeviceId);
            cmd.Parameters.AddWithValue("@ProfileId", (object?)searchParams.ProfileId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ObjectType", (object?)searchParams.ObjectType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            cmd.Parameters.AddWithValue("@IntervalMinutes", intervalMinutes);

            var allReadings = new List<ParameterValueSearch>();
            using var reader = cmd.ExecuteReader();
            long rowId = 1;
            while (reader.Read())
            {
                allReadings.Add(new ParameterValueSearch
                {
                    Id = rowId++,
                    ParameterId = reader.GetInt32(0),
                    ParameterName = reader.GetString(1),
                    DateStamp = reader.GetDateTime(2),
                    Value = reader.GetString(3),
                    DeviceName = ""
                });
            }

            var timestamps = allReadings
                .Where(r => r.DateStamp.HasValue)
                .Select(r => r.DateStamp!.Value)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            int totalTimestamps = timestamps.Count;

            var pagedTimestamps = pageSize == int.MaxValue
                ? timestamps
                : timestamps.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var pagedTimestampsSet = new HashSet<DateTime>(pagedTimestamps);
            var pagedReadings = allReadings.Where(r => r.DateStamp.HasValue && pagedTimestampsSet.Contains(r.DateStamp.Value)).ToList();

            return (totalTimestamps, pagedReadings);
        }
    }
}
