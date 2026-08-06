using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/devicelog")]
    public class ParameterValueController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ParameterValueController> _logger;
        private readonly DataContext _db;

        // The "yyyy-MM-dd HH:mm:ss" format is the fixed contract enforced by
        // ValueFormatter.FormatValue in the sync infrastructure.
        private const string ClockStringFormat = "yyyy-MM-dd HH:mm:ss";

        // OBIS code for the clock/timestamp parameter stored in ReadingValues.
        private const string ClockObisCode = "0.0.1.0.0.255";

        public ParameterValueController(ILogger<ParameterValueController> logger, DataContext db)
        {
            _logger = logger;
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet("Search")]
        public ActionResult Search([FromQuery] SearchParams searchParams)
        {
            try
            {
                // Server-side validation: Reject inverted date ranges
                if (searchParams.StartDate != default && searchParams.EndDate != default && searchParams.StartDate > searchParams.EndDate)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string> { "Start Date cannot be after End Date." };
                    return Ok(_apiResponse);
                }

                // Default pagination values if invalid
                int pageNumber = searchParams.PageNumber > 0 ? searchParams.PageNumber : 1;
                // PageSize now = max distinct timestamps (columns) per page
                int pageSize = searchParams.PageSize > 0 ? searchParams.PageSize : 20;

                // Build filtered session list & reading values
                var (totalTimestamps, resultsList) = FetchParameterReadings(_db, searchParams, pageNumber, pageSize);

                var result = new ParameterValueSearchResult
                {
                    DeviceLogSearch = resultsList,
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
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpGet("Export")]
        public IActionResult Export([FromQuery] SearchParams searchParams, [FromQuery] string format = "csv")
        {
            try
            {
                // Fetch ALL readings matching criteria (no pagination limit)
                var (_, readings) = FetchParameterReadings(_db, searchParams, pageNumber: 1, pageSize: int.MaxValue);

                // Pivot the readings: Rows = Parameters, Columns = Timestamps
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

                // Build CSV string with UTF-8 BOM so Excel opens it with correct formatting
                var sb = new StringBuilder();
                
                // Header row: Parameter, timestamp1, timestamp2...
                sb.Append("\"Parameter\"");
                foreach (var ts in timestamps)
                {
                    sb.Append($",\"{ts:yyyy-MM-dd HH:mm:ss}\"");
                }
                sb.AppendLine();

                // Data rows
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

                string fileName = $"DeviceReadings_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

                if (string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
                {
                    return File(bytes, "application/vnd.ms-excel", fileName.Replace(".csv", ".xls"));
                }
                else
                {
                    return File(bytes, "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting parameter readings");
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        private (int TotalTimestamps, List<ParameterValueSearch> Results) FetchParameterReadings(
            DataContext db, SearchParams searchParams, int pageNumber, int pageSize)
        {
            // --- Build effective parameter ID filter ---
            var effectiveParamIds = new HashSet<int>();
            if (searchParams.ParameterIds != null)
            {
                foreach (var id in searchParams.ParameterIds.Where(id => id > 0))
                    effectiveParamIds.Add(id);
            }
            if (searchParams.ParameterId > 0)
                effectiveParamIds.Add(searchParams.ParameterId);

            List<int>? expandedParamIds = null;
            if (effectiveParamIds.Count > 0)
            {
                expandedParamIds = new List<int>();
                foreach (var targetId in effectiveParamIds)
                {
                    var targetParam = db.Parameter
                        .Where(p => p.Id == targetId)
                        .Select(p => new { p.Id, p.ObisCode, p.Name })
                        .FirstOrDefault();

                    if (targetParam == null)
                    {
                        expandedParamIds.Add(targetId);
                        continue;
                    }

                    var matches = db.Parameter
                        .Where(p =>
                            (!string.IsNullOrEmpty(targetParam.ObisCode) && p.ObisCode == targetParam.ObisCode) ||
                            (p.Name != null && p.Name == targetParam.Name))
                        .Select(p => p.Id)
                        .ToList();

                    expandedParamIds.AddRange(matches);
                }
                expandedParamIds = expandedParamIds.Distinct().ToList();
            }

            // Lookup device timezone for UTC conversion
            Device? device = null;
            if (searchParams.DeviceId > 0)
                device = db.Device.FirstOrDefault(d => d.Id == searchParams.DeviceId && !d.IsDeleted);

            TimeZoneInfo deviceTz = TimeZoneInfo.Utc;
            if (device != null && !string.IsNullOrWhiteSpace(device.TimeZoneId))
            {
                try { deviceTz = TimeZoneInfo.FindSystemTimeZoneById(device.TimeZoneId); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve TimeZoneId {TimeZoneId} for device {DeviceId}, falling back to UTC.",
                        device.TimeZoneId, device.Id);
                }
            }

            // Pre-fetch clock parameter IDs
            var clockParamIds = db.Parameter
                .Where(p => p.ObisCode == ClockObisCode)
                .Select(p => p.Id)
                .ToList();

            // --- Build filtered session query ---
            var sessionQuery = db.ReadingSessions.AsQueryable();

            if (searchParams.DeviceId > 0)
                sessionQuery = sessionQuery.Where(rs => rs.DeviceId == searchParams.DeviceId);

            if (searchParams.ProfileId.HasValue && searchParams.ProfileId.Value > 0)
                sessionQuery = sessionQuery.Where(rs => rs.ProfileId == searchParams.ProfileId.Value);

            if (searchParams.StartDate != default)
            {
                var unspecifiedStart = DateTime.SpecifyKind(searchParams.StartDate, DateTimeKind.Unspecified);
                DateTime startUtc = searchParams.StartDate.Kind == DateTimeKind.Utc
                    ? searchParams.StartDate
                    : TimeZoneInfo.ConvertTimeToUtc(unspecifiedStart, deviceTz);

                sessionQuery = sessionQuery.Where(rs =>
                    rs.EntryTimestampUtc != null
                        ? rs.EntryTimestampUtc >= startUtc
                        : rs.ReadTime >= searchParams.StartDate);
            }

            if (searchParams.EndDate != default)
            {
                DateTime endDate = searchParams.EndDate.TimeOfDay == TimeSpan.Zero
                    ? searchParams.EndDate.Date.AddDays(1).AddTicks(-1)
                    : searchParams.EndDate;

                var unspecifiedEnd = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified);
                DateTime endUtc = endDate.Kind == DateTimeKind.Utc
                    ? endDate
                    : TimeZoneInfo.ConvertTimeToUtc(unspecifiedEnd, deviceTz);

                sessionQuery = sessionQuery.Where(rs =>
                    rs.EntryTimestampUtc != null
                        ? rs.EntryTimestampUtc <= endUtc
                        : rs.ReadTime <= endDate);
            }

            if (expandedParamIds != null && expandedParamIds.Count > 0)
            {
                sessionQuery = sessionQuery.Where(rs =>
                    db.ReadingValues.Any(rv => rv.SessionId == rs.Id && rv.ParameterId.HasValue && expandedParamIds.Contains(rv.ParameterId.Value)));
            }

            // --- Paginate by distinct timestamps (columns) ---
            var allSessionsOrdered = sessionQuery
                .OrderBy(rs => rs.EntryTimestampUtc ?? rs.ReadTime)
                .Select(rs => rs.Id)
                .ToList();

            int totalTimestamps = allSessionsOrdered.Count;

            var pageSessionIds = pageSize == int.MaxValue
                ? allSessionsOrdered
                : allSessionsOrdered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            if (pageSessionIds.Count == 0)
            {
                return (totalTimestamps, new List<ParameterValueSearch>());
            }

            var readingQuery = from rv in db.ReadingValues
                               join rs in db.ReadingSessions on rv.SessionId equals rs.Id
                               join d in db.Device on rs.DeviceId equals d.Id
                               join p in db.Parameter on rv.ParameterId equals p.Id
                               where pageSessionIds.Contains(rs.Id)
                               select new
                               {
                                   Id = rv.Id,
                                   Value = rv.Value ?? "",
                                   SessionId = rs.Id,
                                   DeviceName = d.Name,
                                   DeviceId = d.Id,
                                   ParameterName = p.Name,
                                   ParameterId = p.Id,
                                   ReadTime = rs.ReadTime,
                                   EntryTimestampUtc = rs.EntryTimestampUtc
                               };

            if (expandedParamIds != null && expandedParamIds.Count > 0)
                readingQuery = readingQuery.Where(q => expandedParamIds.Contains(q.ParameterId));

            var pagedRows = readingQuery.ToList();

            var clockMap = db.ReadingValues
                .Where(cv => cv.SessionId.HasValue
                    && pageSessionIds.Contains(cv.SessionId.Value)
                    && cv.ParameterId.HasValue
                    && clockParamIds.Contains(cv.ParameterId.Value))
                .Select(cv => new { SessionId = cv.SessionId!.Value, cv.Value })
                .ToList()
                .GroupBy(cv => cv.SessionId)
                .ToDictionary(g => g.Key, g => g.First().Value);

            var resultsList = pagedRows.Select(x =>
            {
                string? clockStr = clockMap.TryGetValue(x.SessionId, out var val) ? val : null;
                DateTime? dateStamp = null;

                if (!string.IsNullOrEmpty(clockStr) &&
                    DateTime.TryParseExact(clockStr, ClockStringFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    dateStamp = parsed;
                }
                else if (x.ReadTime.HasValue)
                {
                    dateStamp = x.ReadTime.Value;
                }

                return new ParameterValueSearch
                {
                    Id = x.Id,
                    Value = ValueFormatter.CleanValue(x.Value),
                    DateStamp = dateStamp,
                    DeviceName = x.DeviceName,
                    ParameterName = x.ParameterName,
                    ParameterId = x.ParameterId
                };
            }).ToList();

            return (totalTimestamps, resultsList);
        }
    }

    public class ParameterValueSearch
    {
        public long Id { get; set; }
        public required string Value { get; set; }
        public DateTime? DateStamp { get; set; }
        public required string DeviceName { get; set; }
        public required string ParameterName { get; set; }
        public int ParameterId { get; set; }
    }

    public class ParameterValueSearchResult
    {
        public int TotalCount { get; set; }
        public List<ParameterValueSearch> DeviceLogSearch { get; set; } = new();
    }
}
