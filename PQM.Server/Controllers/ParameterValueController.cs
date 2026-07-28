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
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/devicelog")]
    public class ParameterValueController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ParameterValueController> _logger;
        private readonly string _connectionString;

        // The "yyyy-MM-dd HH:mm:ss" format is the fixed contract enforced by
        // ValueFormatter.FormatValue in the sync infrastructure.
        private const string ClockStringFormat = "yyyy-MM-dd HH:mm:ss";

        // OBIS code for the clock/timestamp parameter stored in ReadingValues.
        private const string ClockObisCode = "0.0.1.0.0.255";

        public ParameterValueController(ILogger<ParameterValueController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet("Search")]
        public ActionResult Search([FromQuery] SearchParams searchParams)
        {
            try
            {
                using var db = new DataContext(_connectionString);

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
                int pageSize = searchParams.PageSize > 0 ? searchParams.PageSize : 10;

                // Lookup device TimeZoneId for UTC conversion if deviceId is specified
                Device? device = null;
                if (searchParams.DeviceId > 0)
                {
                    device = db.Device.FirstOrDefault(d => d.Id == searchParams.DeviceId && !d.IsDeleted);
                }

                TimeZoneInfo deviceTz = TimeZoneInfo.Utc;
                if (device != null && !string.IsNullOrWhiteSpace(device.TimeZoneId))
                {
                    try
                    {
                        deviceTz = TimeZoneInfo.FindSystemTimeZoneById(device.TimeZoneId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve TimeZoneId {TimeZoneId} for device {DeviceId}, falling back to UTC.", device.TimeZoneId, device.Id);
                    }
                }

                // Pre-fetch parameter IDs for the clock OBIS code
                var clockParamIds = db.Parameter
                    .Where(p => p.ObisCode == ClockObisCode)
                    .Select(p => p.Id)
                    .ToList();

                // Build filtered session query first to leverage database indexes (DeviceId & EntryTimestampUtc)
                var sessionQuery = db.ReadingSessions.AsQueryable();

                if (searchParams.DeviceId > 0)
                {
                    sessionQuery = sessionQuery.Where(rs => rs.DeviceId == searchParams.DeviceId);
                }

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
                    var endDate = searchParams.EndDate.Date.AddDays(1).AddTicks(-1);
                    var unspecifiedEnd = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified);
                    DateTime endUtc = endDate.Kind == DateTimeKind.Utc
                        ? endDate
                        : TimeZoneInfo.ConvertTimeToUtc(unspecifiedEnd, deviceTz);

                    sessionQuery = sessionQuery.Where(rs =>
                        rs.EntryTimestampUtc != null
                            ? rs.EntryTimestampUtc <= endUtc
                            : rs.ReadTime <= endDate);
                }

                // Join matching sessions with ReadingValues, Device, and Parameter
                var tempQuery = from rs in sessionQuery
                                join rv in db.ReadingValues on rs.Id equals rv.SessionId
                                join d in db.Device on rs.DeviceId equals d.Id
                                join p in db.Parameter on rv.ParameterId equals p.Id
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

                // Optional parameter filter with multi-ID matching (matches OBIS code or Name safely)
                if (searchParams.ParameterId > 0)
                {
                    var targetParam = db.Parameter
                        .Where(p => p.Id == searchParams.ParameterId)
                        .Select(p => new { p.Id, p.ObisCode, p.Name })
                        .FirstOrDefault();

                    if (targetParam != null)
                    {
                        var matchingParamIds = db.Parameter
                            .Where(p => (!string.IsNullOrEmpty(targetParam.ObisCode) && p.ObisCode == targetParam.ObisCode) || (p.Name != null && p.Name == targetParam.Name))
                            .Select(p => p.Id)
                            .ToList();

                        tempQuery = tempQuery.Where(q => matchingParamIds.Contains(q.ParameterId));
                    }
                    else
                    {
                        tempQuery = tempQuery.Where(q => q.ParameterId == searchParams.ParameterId);
                    }
                }

                var totalCount = tempQuery.Count();

                // Materialize the target page of rows
                var pagedRows = tempQuery
                    .OrderByDescending(q => q.EntryTimestampUtc ?? q.ReadTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Batch-lookup meter clock values for the session IDs on this page
                var sessionIds = pagedRows
                    .Select(r => r.SessionId)
                    .Distinct()
                    .ToList();

                var clockMap = db.ReadingValues
                    .Where(cv => cv.SessionId.HasValue
                        && sessionIds.Contains(cv.SessionId.Value)
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

                var result = new ParameterValueSearchResult
                {
                    DeviceLogSearch = resultsList,
                    TotalCount = totalCount
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
