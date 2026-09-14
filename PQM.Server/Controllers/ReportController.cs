using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Models;
using System.Text;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class ReportController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly IReportRepository _reportRepository;

        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        [HttpGet("aggregate")]
        public IActionResult GetAggregatedReport([FromQuery] ReportSearch searchParams)
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

                var (totalTimestamps, results) = _reportRepository.GetAggregatedReport(searchParams, interval, pageNumber, pageSize);

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
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpGet("export")]
        public IActionResult ExportAggregatedReport([FromQuery] ReportSearch searchParams)
        {
            try
            {
                if (searchParams.DeviceId <= 0)
                {
                    return BadRequest(new { status = false, message = "DeviceId is required." });
                }

                int interval = searchParams.IntervalMinutes > 0 ? searchParams.IntervalMinutes : 15;
                var (_, readings) = _reportRepository.GetAggregatedReport(searchParams, interval, pageNumber: 1, pageSize: int.MaxValue);

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

                sb.AppendLine($"\"Report Type\",\"Interval Aggregated Report ({interval} min buckets)\"");
                sb.AppendLine($"\"Generated At\",\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
                sb.AppendLine();

                sb.Append("\"Parameter\"");
                foreach (var ts in timestamps)
                {
                    sb.Append($",\"{ts:yyyy-MM-dd HH:mm:ss}\"");
                }
                sb.AppendLine();

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
                return BadRequest(new { status = false, message = ex.Message });
            }
        }
    }
}