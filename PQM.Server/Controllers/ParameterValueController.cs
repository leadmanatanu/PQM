using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
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
        // ValueFormatter.FormatValue in the sync infrastructure. The string-based
        // date filtering below (ClockString.CompareTo) is only correct because this
        // format sorts lexicographically identically to chronological order.
        // WARNING: if ValueFormatter ever changes its clock output format, date
        // range filtering in this endpoint will silently produce wrong results.
        private const string ClockStringFormat = "yyyy-MM-dd HH:mm:ss";

        // OBIS code for the clock/timestamp parameter stored in ReadingValues.
        // Each ReadingSession row has one ReadingValue whose Parameter.ObisCode == this value,
        // and its Value column contains the meter's raw local clock string ("yyyy-MM-dd HH:mm:ss").
        // This is what the frontend should display as "when this reading occurred" — NOT ReadTime
        // (sync execution time) and NOT EntryTimestampUtc (backend-only UTC watermark).
        private const string ClockObisCode = "0.0.1.0.0.255";

        public ParameterValueController(ILogger<ParameterValueController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get()
        {
            using var db = new DataContext(_connectionString);
            // Rerouted from legacy db.ParameterValue (table no longer exists) to ReadingValues.
            var data = db.ReadingValues
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpPost]
        public ActionResult Post([FromBody] List<ParameterValue> values)
        {
            // Write path retained for backward compatibility.
            // New sync logic writes directly via the infrastructure layer, not this endpoint.
            try
            {
                if (values == null || values.Count == 0)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }

                using var db = new DataContext(_connectionString);
                db.ParameterValue.AddRange(values);
                db.SaveChanges();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = values.Count;
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

        [HttpGet("Search")]
        public ActionResult Search([FromQuery] SearchParams searchParams)
        {
            try
            {
                using var db = new DataContext(_connectionString);

                // Build a flat projection that includes the clock string for this session via a
                // LEFT JOIN correlated subquery. Using a correlated let-subquery rather than a
                // true LINQ join ensures sessions with no clock reading still appear (null clockVal)
                // rather than being silently dropped.
                var tempQuery = from rv in db.ReadingValues
                                join rs in db.ReadingSessions on rv.SessionId equals rs.Id
                                join d in db.Device on rs.DeviceId equals d.Id
                                join p in db.Parameter on rv.ParameterId equals p.Id
                                where rs.DeviceId == searchParams.DeviceId

                                // Left-join equivalent: fetch the meter's raw local clock string for
                                // this session. This string (format "yyyy-MM-dd HH:mm:ss", produced
                                // by ValueFormatter) is the correct "when this reading occurred" value
                                // per the display contract — NOT ReadTime (sync execution time).
                                let clockVal = db.ReadingValues
                                    .Where(cv => cv.SessionId == rs.Id
                                        && cv.Parameter!.ObisCode == ClockObisCode)
                                    .Select(cv => cv.Value)
                                    .FirstOrDefault()

                                select new
                                {
                                    Id = rv.Id,
                                    Value = rv.Value ?? "",
                                    ClockString = clockVal,     // nullable: null when clock entry missing
                                    DeviceName = d.Name,
                                    ParameterName = p.Name,
                                    ParameterId = p.Id,
                                    ReadTime = rs.ReadTime      // sync execution time — fallback only
                                };

                if (searchParams.ParameterId > 0)
                {
                    tempQuery = tempQuery.Where(q => q.ParameterId == searchParams.ParameterId);
                }

                // String-based date filtering: only correct because ValueFormatter always produces
                // the fixed "yyyy-MM-dd HH:mm:ss" format, which sorts lexicographically = chronologically.
                // A format change in ValueFormatter would silently break these comparisons.
                if (searchParams.StartDate != default)
                {
                    string startStr = searchParams.StartDate.ToString(ClockStringFormat);
                    tempQuery = tempQuery.Where(q =>
                        q.ClockString != null
                            ? string.Compare(q.ClockString, startStr) >= 0
                            : q.ReadTime >= searchParams.StartDate);
                }

                if (searchParams.EndDate != default)
                {
                    var endDate = searchParams.EndDate.Date.AddDays(1).AddTicks(-1);
                    string endStr = endDate.ToString(ClockStringFormat);
                    tempQuery = tempQuery.Where(q =>
                        q.ClockString != null
                            ? string.Compare(q.ClockString, endStr) <= 0
                            : q.ReadTime <= endDate);
                }

                var totalCount = tempQuery.Count();

                // Sort by clock string first (meter-recorded time), with ReadTime as tiebreaker.
                // Ordering by ClockString descending on the DB keeps pagination correct.
                var items = tempQuery
                    .OrderByDescending(q => q.ClockString ?? "")
                    .ThenByDescending(q => q.ReadTime)
                    .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
                    .Take(searchParams.PageSize)
                    .ToList();  // materialise here; parsing happens in-memory below

                // Safe in-memory clock string → DateTime? parsing.
                // Fallback chain: parsed meter clock → ReadTime → null (never DateTime.UtcNow).
                // The frontend renders null DateStamp as '-' (devices-table.tsx line 114).
                var resultsList = items.Select(x =>
                {
                    DateTime? dateStamp = null;

                    if (!string.IsNullOrEmpty(x.ClockString) &&
                        DateTime.TryParseExact(x.ClockString, ClockStringFormat,
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        dateStamp = parsed;
                    }
                    else if (x.ReadTime.HasValue)
                    {
                        // ReadTime is sync-execution time, not meter recording time.
                        // Used only as a last resort when the clock value is missing or unparseable.
                        dateStamp = x.ReadTime.Value;
                    }
                    // If both are unavailable, dateStamp remains null — frontend shows '-'.

                    return new ParameterValueSearch
                    {
                        Id = x.Id,
                        Value = x.Value,
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

        // Nullable: represents the meter's own local clock string parsed to DateTime.
        // Null when the clock entry is missing for a session and ReadTime is also unavailable.
        // The frontend renders null as '-' (devices-table.tsx: row.dateStamp ? ... : '-').
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
