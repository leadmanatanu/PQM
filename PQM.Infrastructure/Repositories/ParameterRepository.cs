using Microsoft.EntityFrameworkCore;
using PQM.Core.DTOs;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PQM.Infrastructure.Repositories
{
    public class ParameterRepository : IParameterRepository
    {
        private readonly DataContext _db;
        private const string ClockStringFormat = "yyyy-MM-dd HH:mm:ss";
        private const string ClockObisCode = "0.0.1.0.0.255";

        public ParameterRepository(DataContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<IEnumerable<Parameter>> GetVisibleParametersAsync(int? deviceId = null, CancellationToken cancellationToken = default)
        {
            var query = _db.Parameter.Where(p => p.IsVisible);

            if (deviceId.HasValue && deviceId.Value > 0)
            {
                var device = await _db.Device.FirstOrDefaultAsync(d => d.Id == deviceId.Value && !d.IsDeleted, cancellationToken);
                if (device != null && device.MeterTypeId.HasValue && device.MeterTypeId.Value > 0)
                {
                    int mtId = device.MeterTypeId.Value;
                    if (mtId == 1) // ABT
                    {
                        query = query.Where(p => p.MeterTypeId == 1 || p.MeterTypeId == 3 || p.MeterTypeId == null);
                    }
                    else if (mtId == 2) // PQ
                    {
                        query = query.Where(p => p.MeterTypeId == 2 || p.MeterTypeId == 3 || p.MeterTypeId == null);
                    }
                }
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<object>> GetDeviceLatestReadingsAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var raw = await _db.DeviceLatestReadings
                .Where(x => x.DeviceId == deviceId)
                .Join(_db.Parameter,
                    x => x.ParameterId,
                    p => p.Id,
                    (x, p) => new
                    {
                        Id = (long)x.ParameterId,
                        ParameterId = x.ParameterId,
                        ParameterName = p.Name,
                        ObisCode = p.ObisCode ?? "",
                        Value = ValueFormatter.CleanValue(x.Value),
                        Timestamp = x.UpdatedAt
                    })
                .ToListAsync(cancellationToken);

            return raw.Cast<object>();
        }

        public async Task<(IEnumerable<ParameterValueSearchDto> Items, int TotalCount)> SearchReadingsAsync(
            int deviceId,
            int parameterId,
            DateTime startDate,
            DateTime endDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int pageNum = pageNumber > 0 ? pageNumber : 1;
            int pageSz = pageSize > 0 ? pageSize : 10;

            Device? device = null;
            if (deviceId > 0)
            {
                device = await _db.Device.FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted, cancellationToken);
            }

            TimeZoneInfo deviceTz = TimeZoneInfo.Utc;
            if (device != null && !string.IsNullOrWhiteSpace(device.TimeZoneId))
            {
                try
                {
                    deviceTz = TimeZoneInfo.FindSystemTimeZoneById(device.TimeZoneId);
                }
                catch
                {
                    // Fall back to UTC
                }
            }

            var clockParamIds = await _db.Parameter
                .Where(p => p.ObisCode == ClockObisCode)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var sessionQuery = _db.ReadingSessions.AsQueryable();

            if (deviceId > 0)
            {
                sessionQuery = sessionQuery.Where(rs => rs.DeviceId == deviceId);
            }

            if (startDate != default)
            {
                var unspecifiedStart = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified);
                DateTime startUtc = startDate.Kind == DateTimeKind.Utc
                    ? startDate
                    : TimeZoneInfo.ConvertTimeToUtc(unspecifiedStart, deviceTz);

                sessionQuery = sessionQuery.Where(rs =>
                    rs.EntryTimestampUtc != null
                        ? rs.EntryTimestampUtc >= startUtc
                        : rs.ReadTime >= startDate);
            }

            if (endDate != default)
            {
                var endDateAdjusted = endDate.Date.AddDays(1).AddTicks(-1);
                var unspecifiedEnd = DateTime.SpecifyKind(endDateAdjusted, DateTimeKind.Unspecified);
                DateTime endUtc = endDateAdjusted.Kind == DateTimeKind.Utc
                    ? endDateAdjusted
                    : TimeZoneInfo.ConvertTimeToUtc(unspecifiedEnd, deviceTz);

                sessionQuery = sessionQuery.Where(rs =>
                    rs.EntryTimestampUtc != null
                        ? rs.EntryTimestampUtc <= endUtc
                        : rs.ReadTime <= endDateAdjusted);
            }

            var tempQuery = from rs in sessionQuery
                            join rv in _db.ReadingValues on rs.Id equals rv.SessionId
                            join d in _db.Device on rs.DeviceId equals d.Id
                            join p in _db.Parameter on rv.ParameterId equals p.Id
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

            if (parameterId > 0)
            {
                var targetParam = await _db.Parameter
                    .Where(p => p.Id == parameterId)
                    .Select(p => new { p.Id, p.ObisCode, p.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (targetParam != null)
                {
                    var matchingParamIds = await _db.Parameter
                        .Where(p => (!string.IsNullOrEmpty(targetParam.ObisCode) && p.ObisCode == targetParam.ObisCode) || (p.Name != null && p.Name == targetParam.Name))
                        .Select(p => p.Id)
                        .ToListAsync(cancellationToken);

                    tempQuery = tempQuery.Where(q => matchingParamIds.Contains(q.ParameterId));
                }
                else
                {
                    tempQuery = tempQuery.Where(q => q.ParameterId == parameterId);
                }
            }

            var totalCount = await tempQuery.CountAsync(cancellationToken);

            var pagedRows = await tempQuery
                .OrderByDescending(q => q.EntryTimestampUtc ?? q.ReadTime)
                .Skip((pageNum - 1) * pageSz)
                .Take(pageSz)
                .ToListAsync(cancellationToken);

            var sessionIds = pagedRows
                .Select(r => r.SessionId)
                .Distinct()
                .ToList();

            var clockValuesRaw = await _db.ReadingValues
                .Where(cv => cv.SessionId.HasValue
                    && sessionIds.Contains(cv.SessionId.Value)
                    && cv.ParameterId.HasValue
                    && clockParamIds.Contains(cv.ParameterId.Value))
                .Select(cv => new { SessionId = cv.SessionId!.Value, cv.Value })
                .ToListAsync(cancellationToken);

            var clockMap = clockValuesRaw
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

                return new ParameterValueSearchDto
                {
                    Id = x.Id,
                    Value = ValueFormatter.CleanValue(x.Value),
                    DateStamp = dateStamp,
                    DeviceName = x.DeviceName,
                    ParameterName = x.ParameterName,
                    ParameterId = x.ParameterId
                };
            }).ToList();

            return (resultsList, totalCount);
        }
    }
}
