using Microsoft.EntityFrameworkCore;
using PQM.Core.DTOs;
using PQM.Core.Entities;
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

        public AggregatedReportResult GetAggregatedReport(
    ReportSearch searchParams, int intervalMinutes)
        {
            DateTime startDate = searchParams.StartDate != default
                ? searchParams.StartDate
                : new DateTime(2000, 1, 1);

            DateTime endDate = searchParams.EndDate != default
                ? (searchParams.EndDate.TimeOfDay == TimeSpan.Zero
                    ? searchParams.EndDate.Date.AddDays(1).AddTicks(-1)
                    : searchParams.EndDate)
                : GetIndiaStandardTime().AddDays(1);

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified);

            string paramIdsCsv = searchParams.ParameterIds?.Count > 0
                ? string.Join(",", searchParams.ParameterIds.Where(x => x > 0))
                : "";

            string profileIdsCsv = searchParams.ProfileIds?.Count > 0
                ? string.Join(",", searchParams.ProfileIds.Where(x => x > 0))
                : "";

            int pageNumber = searchParams.PageNumber > 0 ? searchParams.PageNumber : 1;
            int pageSize = searchParams.PageSize > 0 ? searchParams.PageSize : 50;
            int offset = (pageNumber - 1) * pageSize;
            int blockLoadId = searchParams.BlockLoadProfileId ?? -1;

            var sql = @"
    WITH BlockLoadTimestamps AS
    (
        SELECT BucketTimestamp,
               ROW_NUMBER() OVER (ORDER BY BucketTimestamp) AS RowNum
        FROM
        (
            SELECT DISTINCT
                DATEADD(
                    minute,
                    (DATEDIFF(minute, '2000-01-01', rs.EntryTimestamp) / {6}) * {6},
                    '2000-01-01'
                ) AS BucketTimestamp
            FROM ReadingValues rv
            INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
            INNER JOIN Parameters p ON rv.ParameterId = p.Id
            WHERE rs.DeviceId = {0}
              AND p.ProfileId = {7}
              AND ({1} = '' OR p.ProfileId IN
                    (SELECT CAST(value AS INT)
                     FROM STRING_SPLIT({1}, ',')))
              AND ({2} IS NULL OR p.ObjectType = {2})
              AND ({3} = '' OR p.Id IN
                    (SELECT CAST(value AS INT)
                     FROM STRING_SPLIT({3}, ',')))
              AND rs.EntryTimestamp >= {4}
              AND rs.EntryTimestamp <= {5}
              AND rv.ValueNumeric IS NOT NULL
        ) t
    ),
    PagedBlockLoad AS
    (
        SELECT BucketTimestamp
        FROM BlockLoadTimestamps
        WHERE RowNum > {8}
          AND RowNum <= {8} + {9}
    ),
    BlockLoadData AS
    (
        SELECT
            p.Id AS ParameterId,
            p.Name AS ParameterName,
            p.ProfileId AS ProfileId,
            p.ObjectType,
            p.AggregationType,
            rv.ValueNumeric AS ScaledValueNumeric,
            DATEADD(
                minute,
                (DATEDIFF(minute, '2000-01-01', rs.EntryTimestamp) / {6}) * {6},
                '2000-01-01'
            ) AS BucketTimestamp
        FROM ReadingValues rv

        INNER JOIN Parameters p ON rv.ParameterId = p.Id
        INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
        
        INNER JOIN PagedBlockLoad pb
            ON pb.BucketTimestamp = DATEADD(
                minute,
                (DATEDIFF(minute, '2000-01-01', rs.EntryTimestamp) / {6}) * {6},
                '2000-01-01'
            )
        WHERE rs.DeviceId = {0}
          AND p.ProfileId = {7}
          AND ({1} = '' OR p.ProfileId IN
                (SELECT CAST(value AS INT)
                 FROM STRING_SPLIT({1}, ',')))
          AND ({2} IS NULL OR p.ObjectType = {2})
          AND ({3} = '' OR p.Id IN
                (SELECT CAST(value AS INT)
                 FROM STRING_SPLIT({3}, ',')))
          AND rs.EntryTimestamp >= {4}
          AND rs.EntryTimestamp <= {5}
          AND rv.ValueNumeric IS NOT NULL
    ),
    OtherProfileData AS
    (
        SELECT
            p.Id AS ParameterId,
            p.Name AS ParameterName,
            p.ProfileId,
            p.ObjectType,
            p.AggregationType,
            rv.ValueNumeric AS ScaledValueNumeric,
            DATEADD(
                minute,
                (DATEDIFF(minute, '2000-01-01', rs.EntryTimestamp) / {6}) * {6},
                '2000-01-01'
            ) AS BucketTimestamp
        FROM ReadingValues rv
        INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
        INNER JOIN Parameters p ON rv.ParameterId = p.Id
        WHERE rs.DeviceId = {0}
          AND (p.ProfileId IS NULL OR p.ProfileId <> {7})
          AND ({1} = '' OR p.ProfileId IN
                (SELECT CAST(value AS INT)
                 FROM STRING_SPLIT({1}, ',')))
          AND ({2} IS NULL OR p.ObjectType = {2})
          AND ({3} = '' OR p.Id IN
                (SELECT CAST(value AS INT)
                 FROM STRING_SPLIT({3}, ',')))
          AND rs.EntryTimestamp >= {4}
          AND rs.EntryTimestamp <= {5}
          AND rv.ValueNumeric IS NOT NULL
    ),
    AllData AS
    (
        SELECT * FROM BlockLoadData
        UNION ALL
        SELECT * FROM OtherProfileData
    ),
    Aggregated AS
    (
        SELECT
            ParameterId,
            ParameterName,
            ProfileId,
            BucketTimestamp,
            CASE
                WHEN AggregationType = 'Max'
                  OR ParameterName LIKE 'Cum%'
                  OR ParameterName LIKE 'Cumulative%'
                THEN CAST(ROUND(MAX(ScaledValueNumeric), 2) AS VARCHAR(50))
                ELSE CAST(ROUND(AVG(ScaledValueNumeric), 2) AS VARCHAR(50))
            END AS Value
        FROM AllData
        GROUP BY
            ParameterId,
            ParameterName,
            ProfileId,
            AggregationType,
            BucketTimestamp
    )
    SELECT
        ParameterId,
        ParameterName,
        ProfileId,
        BucketTimestamp AS DateStamp,
        Value
    FROM Aggregated
    ORDER BY ProfileId, ParameterId, BucketTimestamp";

            var rows = _db.Database.SqlQueryRaw<AggregatedReportRow>(
                sql,
                searchParams.DeviceId,
                profileIdsCsv,
                (object?)searchParams.ObjectType ?? DBNull.Value,
                paramIdsCsv,
                startDate,
                endDate,
                intervalMinutes,
                blockLoadId,
                offset,
                pageSize
            ).ToList();

            var items = rows.Select((r, i) => new ParameterValueSearch
            {
                Id = i + 1,
                ParameterId = r.ParameterId,
                ParameterName = r.ParameterName,
                ProfileId = r.ProfileId,
                DateStamp = r.DateStamp,
                Value = r.Value,
                DeviceName = ""
            }).ToList();

            int totalTimestamps = 0;

            if (searchParams.BlockLoadProfileId.HasValue)
            {
                var countSql = @"
    SELECT COUNT(*) AS [Value]
    FROM
    (
        SELECT DISTINCT
            DATEADD(
                minute,
                (
                    DATEDIFF(
                        minute,
                        '2000-01-01',
                        rs.EntryTimestamp
                    ) / {6}
                ) * {6},
                '2000-01-01'
            ) AS BucketTimestamp

        FROM ReadingValues rv

        INNER JOIN ReadingSessions rs
            ON rv.SessionId = rs.Id

        INNER JOIN Parameters p
            ON rv.ParameterId = p.Id

        WHERE rs.DeviceId = {0}

          AND
          (
              {1} = ''
              OR p.ProfileId IN
              (
                  SELECT CAST(value AS INT)
                  FROM STRING_SPLIT({1}, ',')
              )
          )

          AND
          (
              {2} IS NULL
              OR p.ObjectType = {2}
          )

          AND
          (
              {3} = ''
              OR p.Id IN
              (
                  SELECT CAST(value AS INT)
                  FROM STRING_SPLIT({3}, ',')
              )
          )

          AND rs.EntryTimestamp >= {4}
          AND rs.EntryTimestamp <= {5}

          AND p.ProfileId = {7}

          AND rv.ValueNumeric IS NOT NULL
    ) AS BlockLoadTimestamps
";

                totalTimestamps = _db.Database.SqlQueryRaw<int>(
                    countSql,
                    searchParams.DeviceId,
                    profileIdsCsv,
                    (object?)searchParams.ObjectType ?? DBNull.Value,
                    paramIdsCsv,
                    startDate,
                    endDate,
                    intervalMinutes,
                    blockLoadId
                ).FirstOrDefault();
            }

            return new AggregatedReportResult
            {
                Items = items,
                Pagination = searchParams.BlockLoadProfileId.HasValue
                    ? new ReportPagination
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalTimestamps = totalTimestamps,
                        TotalPages = totalTimestamps > 0
                            ? (int)Math.Ceiling(totalTimestamps / (double)pageSize)
                            : 0
                    }
                    : null
            };
        }

        public List<ParameterValueSearch> GetAggregatedReportForExport(
    ReportSearch searchParams,
    int intervalMinutes)
        {
            try
            {
                DateTime startDate = searchParams.StartDate != default
               ? searchParams.StartDate
               : new DateTime(2000, 1, 1);

                DateTime endDate = searchParams.EndDate != default
                    ? (searchParams.EndDate.TimeOfDay == TimeSpan.Zero
                        ? searchParams.EndDate.Date.AddDays(1).AddTicks(-1)
                        : searchParams.EndDate)
                    : GetIndiaStandardTime().AddDays(1);

                startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified);
                endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified);

                string paramIdsCsv = searchParams.ParameterIds != null &&
                                     searchParams.ParameterIds.Count > 0
                    ? string.Join(",", searchParams.ParameterIds.Where(id => id > 0))
                    : "";

                string profileIdsCsv = searchParams.ProfileIds != null &&
                                       searchParams.ProfileIds.Count > 0
                    ? string.Join(",", searchParams.ProfileIds.Where(id => id > 0))
                    : "";

                var sql = @"
                WITH ScaledReadings AS (
                    SELECT
                        p.Id AS ParameterId,
                        p.Name AS ParameterName,
                        p.ProfileId,
                        p.ObjectType,
                        p.AggregationType,
                        rv.ValueNumeric AS ScaledValueNumeric,
                        DATEADD(minute,
                            (DATEDIFF(minute, '2000-01-01', rs.EntryTimestamp) / {6}) * {6},
                            '2000-01-01') AS BucketTimestamp
                    FROM ReadingValues rv
                    INNER JOIN Parameters p ON rv.ParameterId = p.Id
                    INNER JOIN ReadingSessions rs ON rv.SessionId = rs.Id
                    WHERE rs.DeviceId = {0}
                      AND ({1} = '' OR p.ProfileId IN (
                          SELECT CAST(value AS INT) FROM STRING_SPLIT({1}, ',')
                      ))
                      AND ({2} IS NULL OR p.ObjectType = {2})
                      AND ({3} = '' OR p.Id IN (
                          SELECT CAST(value AS INT) FROM STRING_SPLIT({3}, ',')
                      ))
                      AND rs.EntryTimestamp >= {4}
                      AND rs.EntryTimestamp <= {5}
                      AND rv.ValueNumeric IS NOT NULL
                ),
                AggregatedBuckets AS (
                    SELECT
                        ParameterId,
                        ParameterName,
                        ProfileId,
                        BucketTimestamp,
                        CASE
                            WHEN AggregationType = 'Max'
                              OR ParameterName LIKE 'Cum%'
                              OR ParameterName LIKE 'Cumulative%'
                            THEN CAST(ROUND(MAX(ScaledValueNumeric), 2) AS VARCHAR(50))
                            ELSE CAST(ROUND(AVG(ScaledValueNumeric), 2) AS VARCHAR(50))
                        END AS AggregatedValue
                    FROM ScaledReadings
                    GROUP BY ParameterId, ParameterName, ProfileId, AggregationType, BucketTimestamp
                )
                SELECT
                    ParameterId,
                    ParameterName,
                    ProfileId,
                    BucketTimestamp AS DateStamp,
                    AggregatedValue AS Value
                FROM AggregatedBuckets
                ORDER BY ProfileId, ParameterId, BucketTimestamp";

                var rawRows = _db.Database
                    .SqlQueryRaw<AggregatedReportRow>(
                        sql,
                        searchParams.DeviceId,
                        profileIdsCsv,
                        (object?)searchParams.ObjectType ?? DBNull.Value,
                        paramIdsCsv,
                        startDate,
                        endDate,
                        intervalMinutes)
                    .ToList();

                return rawRows
                    .Select((r, idx) => new ParameterValueSearch
                    {
                        Id = idx + 1,
                        ParameterId = r.ParameterId,
                        ParameterName = r.ParameterName,
                        ProfileId = r.ProfileId,
                        DateStamp = r.DateStamp,
                        Value = r.Value,
                        DeviceName = ""
                    })
                    .ToList();
            }
            catch (Exception)
            {

                throw;
            }
           
        }
        public async Task<List<ProfileDropdownDto>> GetProfilesByDeviceIdAsync(int deviceId,CancellationToken cancellationToken)
        {
            return await _db.ReadingSessions
                .Where(rs => rs.DeviceId == deviceId)
                .Join(
                    _db.Profiles,
                    rs => rs.ProfileId,
                    p => p.Id,
                    (rs, p) => new ProfileDropdownDto
                    {
                        Id = p.Id,
                        FriendlyName = p.FriendlyName,
                        ObisCode = p.ObisCode
                    })
                .Distinct()
                .OrderBy(p => p.FriendlyName)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<ParameterDropdownDto>> GetParametersByProfileIdAsync(int profileId,CancellationToken cancellationToken)
        {
            return await _db.Parameter
                .Where(p => p.ProfileId == profileId)
                .OrderBy(p => p.Name)
                .Select(p => new ParameterDropdownDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ObisCode = p.ObisCode,
                    Description = p.Description,
                    DataType = p.DataType,
                    ObjectType = p.ObjectType,
                    AttributeIndex = p.AttributeIndex,
                    ProfileId = p.ProfileId
                })
                .ToListAsync(cancellationToken);
        }
        public async Task<Device> GetDeviceByIdAsync(int deviceId,CancellationToken cancellationToken)
        {
            var device = await _db.Device
                .FirstOrDefaultAsync(
                    d => d.Id == deviceId,
                    cancellationToken);

            if (device == null)
            {
                throw new KeyNotFoundException(
                    $"Device with Id {deviceId} was not found.");
            }

            return device;
        }
        private static DateTime GetIndiaStandardTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        }
    }
}