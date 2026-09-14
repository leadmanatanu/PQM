using Microsoft.EntityFrameworkCore;
using PQM.Core.DTOs;
using PQM.Core.Interfaces.Repositories;

namespace PQM.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly DataContext _db;

        public ReportRepository(DataContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public (int TotalTimestamps, List<ParameterValueSearch> Results) GetAggregatedReport(ReportSearch searchParams,int intervalMinutes,int pageNumber,int pageSize)
        {
            TimeZoneInfo deviceTz = TimeZoneInfo.Utc;

            if (searchParams.DeviceId > 0)
            {
                var timeZoneId = _db.Device
                    .Where(d => d.Id == searchParams.DeviceId && !d.IsDeleted)
                    .Select(d => d.TimeZoneId)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(timeZoneId))
                {
                    try
                    {
                        deviceTz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    }
                    catch
                    {
                        // Fall back to UTC if the stored TimeZoneId is invalid.
                    }
                }
            }

            DateTime startDate = searchParams.StartDate != default
                ? searchParams.StartDate
                : new DateTime(2000, 1, 1);

            DateTime endDate = searchParams.EndDate != default
                ? (searchParams.EndDate.TimeOfDay == TimeSpan.Zero ? searchParams.EndDate.Date.AddDays(1).AddTicks(-1) : searchParams.EndDate)
                : DateTime.UtcNow.AddDays(1);

            var unspecifiedStart = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified);
            DateTime startUtc = startDate.Kind == DateTimeKind.Utc
                ? startDate
                : TimeZoneInfo.ConvertTimeToUtc(unspecifiedStart, deviceTz);

            var unspecifiedEnd = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified);
            DateTime endUtc = endDate.Kind == DateTimeKind.Utc
                ? endDate
                : TimeZoneInfo.ConvertTimeToUtc(unspecifiedEnd, deviceTz);

            string paramIdsCsv = "";
            if (searchParams.ParameterIds != null && searchParams.ParameterIds.Count > 0)
            {
                paramIdsCsv = string.Join(",", searchParams.ParameterIds.Where(id => id > 0));
            }

            var sql = @"
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
                            (DATEDIFF(minute, '2000-01-01', rs.EntryTimestampUtc) / {6}) * {6}, 
                            '2000-01-01') AS BucketTimestamp
                    FROM ReadingValues rv
                    INNER JOIN Parameters p ON rv.ParameterId = p.Id
                    INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
                    WHERE rs.DeviceId = {0}
                      AND ({1} IS NULL OR p.ProfileId = {1})
                      AND ({2} IS NULL OR p.ObjectType = {2})
                      AND ({3} = '' OR p.Id IN (SELECT CAST(value AS INT) FROM STRING_SPLIT({3}, ',')))
                      AND rs.EntryTimestampUtc >= {4}
                      AND rs.EntryTimestampUtc <= {5}
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
                ORDER BY ParameterId, BucketTimestamp";

            var rawRows = _db.Database
                .SqlQueryRaw<AggregatedReportRow>(
                    sql,
                    searchParams.DeviceId,
                    (object?)searchParams.ProfileId ?? DBNull.Value,
                    (object?)searchParams.ObjectType ?? DBNull.Value,
                    paramIdsCsv,
                    startUtc,
                    endUtc,
                    intervalMinutes)
                .ToList();

            var allReadings = rawRows
                .Select((r, idx) => new ParameterValueSearch
                {
                    Id = idx + 1,
                    ParameterId = r.ParameterId,
                    ParameterName = r.ParameterName,
                    DateStamp = r.DateStamp,
                    Value = r.Value,
                    DeviceName = ""
                })
                .ToList();

            var timestamps = allReadings
                .Where(r => r.DateStamp.HasValue)
                .Select(r => r.DateStamp!.Value)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            int totalTimestamps = timestamps.Count;
            if (totalTimestamps == 0)
            {
                return (0, new List<ParameterValueSearch>());
            }

            int validPageNumber = pageNumber;
            if ((validPageNumber - 1) * pageSize >= totalTimestamps)
            {
                validPageNumber = 1;
            }

            var pagedTimestamps = pageSize == int.MaxValue
                ? timestamps
                : timestamps.Skip((validPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var pagedTimestampsSet = new HashSet<DateTime>(pagedTimestamps);
            var pagedReadings = allReadings
                .Where(r => r.DateStamp.HasValue && pagedTimestampsSet.Contains(r.DateStamp.Value))
                .ToList();

            return (totalTimestamps, pagedReadings);
        }
    }
}