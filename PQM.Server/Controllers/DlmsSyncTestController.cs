using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace PQM.Server.Controllers
{
    /// <summary>
    /// TEMPORARY test controller for Stage 4 ProfileSyncService isolation testing.
    /// Will be removed after verification.
    /// </summary>
    [ApiController]
    [Route("api/test/sync")]
    public class DlmsSyncTestController : ControllerBase
    {
        private readonly ProfileSyncService _syncService;
        private readonly string _connectionString;
        private readonly ILogger<DlmsSyncTestController> _logger;

        public DlmsSyncTestController(ProfileSyncService syncService, IConfiguration configuration, ILogger<DlmsSyncTestController> logger)
        {
            _syncService = syncService;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
            _logger = logger;
        }

        /// <summary>
        /// GET /api/test/sync/run-isolation-test/{deviceId}
        /// Runs RUN 1, RUN 2, RUN 3, duplicate check, and latest readings check.
        /// </summary>
        [HttpGet("run-isolation-test/{deviceId:int}")]
        public async Task<IActionResult> RunIsolationTest(int deviceId)
        {
            const string timeSeriesObis = "1.0.99.2.0.255"; // Daily Load profile
            const string staticObis = "1.0.94.91.4.255";     // Scaler: Block Load profile

            _logger.LogInformation("=== STARTING STAGE 4 ISOLATION TEST FOR DEVICE {DeviceId} ===", deviceId);

            // RUN 1: TimeSeries Profile Initial Sync
            _logger.LogInformation("--- RUN 1: TimeSeries Profile Initial Sync ({ObisCode}) ---", timeSeriesObis);
            var run1 = await _syncService.SyncDeviceProfileAsync(deviceId, timeSeriesObis);

            // Check DeviceProfileSyncState after Run 1
            var syncStateAfterRun1 = await GetSyncStateAsync(deviceId, timeSeriesObis);

            // Allow meter TCP socket to reset state
            await System.Threading.Tasks.Task.Delay(0);

            // RUN 2: TimeSeries Profile Immediate 2nd Sync
            _logger.LogInformation("--- RUN 2: TimeSeries Profile Immediate Re-Sync ({ObisCode}) ---", timeSeriesObis);
            var run2 = await _syncService.SyncDeviceProfileAsync(deviceId, timeSeriesObis);

            // Check DeviceProfileSyncState after Run 2
            var syncStateAfterRun2 = await GetSyncStateAsync(deviceId, timeSeriesObis);

            // Allow meter TCP socket to reset state
            await System.Threading.Tasks.Task.Delay(0);

            // RUN 3: Static/Metadata Profile Sync
            _logger.LogInformation("--- RUN 3: Static/Metadata Profile Sync ({ObisCode}) ---", staticObis);
            var run3 = await _syncService.SyncDeviceProfileAsync(deviceId, staticObis);

            // Check DeviceProfileSyncState after Run 3 (should be null / empty for static)
            var syncStateStatic = await GetSyncStateAsync(deviceId, staticObis);

            // DUPLICATE CHECK QUERY
            int duplicateCount = await CountDuplicateSessionsAsync(deviceId);

            // LATEST READINGS COUNT
            int latestReadingsCount = await CountLatestReadingsAsync(deviceId);

            return Ok(new
            {
                DeviceId = deviceId,
                Run1_TimeSeries_Initial = new
                {
                    Result = run1,
                    SyncStateInDb = syncStateAfterRun1
                },
                Run2_TimeSeries_ReSync = new
                {
                    Result = run2,
                    SyncStateInDb = syncStateAfterRun2
                },
                Run3_Static_Profile = new
                {
                    Result = run3,
                    SyncStateInDb = syncStateStatic // Should be null because static profiles never write to DeviceProfileSyncState
                },
                DuplicateCheck = new
                {
                    DuplicateSessionsFound = duplicateCount,
                    Pass = duplicateCount == 0
                },
                DeviceLatestReadings = new
                {
                    TotalCountForDevice = latestReadingsCount
                }
            });
        }

        private async Task<object?> GetSyncStateAsync(int deviceId, string obisCode)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.DeviceId, p.ObisCode, s.LastReadTimestampUtc, s.LastSyncedAt
                FROM DeviceProfileSyncState s
                JOIN Profiles p ON s.ProfileId = p.ProfileId
                WHERE s.DeviceId = @did AND p.ObisCode = @obis";
            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@obis", obisCode);

            using var rdr = await cmd.ExecuteReaderAsync();
            if (await rdr.ReadAsync())
            {
                return new
                {
                    DeviceId = rdr.GetInt32(0),
                    ObisCode = rdr.GetString(1),
                    LastReadTimestampUtc = rdr.IsDBNull(2) ? (DateTime?)null : rdr.GetDateTime(2),
                    LastSyncedAt = rdr.GetDateTime(3)
                };
            }
            return null;
        }

        private async Task<int> CountDuplicateSessionsAsync(int deviceId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM (
                    SELECT DeviceId, ProfileId, EntryTimestampUtc, COUNT(*) AS cnt
                    FROM ReadingSessions
                    WHERE DeviceId = @did AND EntryTimestampUtc IS NOT NULL
                    GROUP BY DeviceId, ProfileId, EntryTimestampUtc
                    HAVING COUNT(*) > 1
                ) dupes";
            cmd.Parameters.AddWithValue("@did", deviceId);

            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        }

        private async Task<int> CountLatestReadingsAsync(int deviceId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM DeviceLatestReadings WHERE DeviceId = @did";
            cmd.Parameters.AddWithValue("@did", deviceId);

            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        }
    }
}
