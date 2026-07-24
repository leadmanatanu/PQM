using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure.Services;
using PQM.Server.Hubs;

namespace PQM.Server.Services
{
    /// <summary>
    /// Background Hosted Service that ticks periodically (~30 seconds) to check
    /// for scheduled device syncs where IsEnabled = 1 and NextRunAtUtc <= UtcNow.
    /// Runs manual sync using ProfileSyncService and streams updates over SignalR.
    /// </summary>
    public class DeviceScheduleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceScheduleRunnerService> _logger;
        private readonly string _connectionString;

        public DeviceScheduleRunnerService(
            IServiceScopeFactory scopeFactory,
            IHubContext<DeviceHub> hubContext,
            IConfiguration configuration,
            ILogger<DeviceScheduleRunnerService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DeviceScheduleRunnerService] Schedule runner started. Ticking every 30s.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndExecuteDueSchedulesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DeviceScheduleRunnerService] Error during schedule check cycle.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("[DeviceScheduleRunnerService] Schedule runner stopped.");
        }

        private async Task CheckAndExecuteDueSchedulesAsync(CancellationToken stoppingToken)
        {
            var dueSchedules = await GetDueSchedulesAsync(stoppingToken);
            if (dueSchedules.Count == 0)
            {
                return;
            }

            _logger.LogInformation("[DeviceScheduleRunnerService] Found {Count} due schedule(s) to execute.", dueSchedules.Count);

            foreach (var item in dueSchedules)
            {
                if (stoppingToken.IsCancellationRequested) break;

                using var scope = _scopeFactory.CreateScope();
                var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

                if (profileSyncService.IsDeviceSyncing(item.DeviceId))
                {
                    _logger.LogInformation(
                        "[DeviceScheduleRunnerService] Device {DeviceId} is currently undergoing a sync. Skipping scheduled run for this tick.",
                        item.DeviceId);
                    continue;
                }

                _logger.LogInformation(
                    "[DeviceScheduleRunnerService] Triggering scheduled sync for Device {DeviceId} (ScheduledTime: {ScheduledTime}, TimeZone: {TimeZoneId})...",
                    item.DeviceId, item.ScheduledTime, item.TimeZoneId);

                // 1. Broadcast SignalR "Syncing" state
                await _hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
                {
                    deviceId = item.DeviceId,
                    status = "Syncing",
                    lastSync = (string?)null,
                    lastError = (string?)null
                }, stoppingToken);

                // 2. Execute sync using shared ProfileSyncService
                var result = await profileSyncService.SyncDeviceAllProfilesAsync(item.DeviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";
                string? finalError = result.ErrorMessage;

                // 3. Broadcast SignalR completion state
                await _hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
                {
                    deviceId = item.DeviceId,
                    status = finalStatus,
                    lastSync = DateTime.UtcNow.ToString("o"),
                    lastError = finalError
                }, stoppingToken);

                // 4. Update DeviceSyncSchedule record (LastRunAtUtc, LastRunStatus, NextRunAtUtc)
                DateTime nowUtc = DateTime.UtcNow;
                DateTime? nextRunAtUtc = ComputeNextRunAtUtc(item.ScheduledTime, item.TimeZoneId, nowUtc);
                string lastRunStatus = result.Success ? "Success" : "Failed";

                await UpdateScheduleCompletionAsync(item.DeviceId, nowUtc, lastRunStatus, nextRunAtUtc, stoppingToken);

                _logger.LogInformation(
                    "[DeviceScheduleRunnerService] Completed scheduled sync for Device {DeviceId}. Status={Status}, NextRunAtUtc={NextRunAtUtc:yyyy-MM-dd HH:mm:ss UTC}",
                    item.DeviceId, lastRunStatus, nextRunAtUtc);
            }
        }

        private class DueScheduleItem
        {
            public int DeviceId { get; set; }
            public TimeSpan ScheduledTime { get; set; }
            public string? TimeZoneId { get; set; }
        }

        private async Task<List<DueScheduleItem>> GetDueSchedulesAsync(CancellationToken cancellationToken)
        {
            var list = new List<DueScheduleItem>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.DeviceId, s.ScheduledTime, d.TimeZoneId
                FROM DeviceSyncSchedule s
                INNER JOIN Devices d ON s.DeviceId = d.Id
                WHERE s.IsEnabled = 1 
                  AND s.NextRunAtUtc IS NOT NULL 
                  AND s.NextRunAtUtc <= @nowUtc
                  AND (d.IsDeleted = 0 OR d.IsDeleted IS NULL)";
            cmd.Parameters.AddWithValue("@nowUtc", DateTime.UtcNow);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DueScheduleItem
                {
                    DeviceId = reader.GetInt32(0),
                    ScheduledTime = reader.GetTimeSpan(1),
                    TimeZoneId = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return list;
        }

        private async Task UpdateScheduleCompletionAsync(int deviceId, DateTime lastRunAtUtc, string lastRunStatus, DateTime? nextRunAtUtc, CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DeviceSyncSchedule
                SET LastRunAtUtc = @lastRunAtUtc,
                    LastRunStatus = @lastRunStatus,
                    NextRunAtUtc = @nextRunAtUtc
                WHERE DeviceId = @deviceId";
            cmd.Parameters.AddWithValue("@deviceId", deviceId);
            cmd.Parameters.AddWithValue("@lastRunAtUtc", lastRunAtUtc);
            cmd.Parameters.AddWithValue("@lastRunStatus", lastRunStatus);
            cmd.Parameters.AddWithValue("@nextRunAtUtc", (object?)nextRunAtUtc ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public static DateTime? ComputeNextRunAtUtc(TimeSpan scheduledTime, string? timeZoneId, DateTime nowUtc)
        {
            TimeZoneInfo tz;
            try
            {
                tz = string.IsNullOrWhiteSpace(timeZoneId)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                tz = TimeZoneInfo.Utc;
            }

            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            DateTime candidateLocal = nowLocal.Date.Add(scheduledTime);

            if (candidateLocal <= nowLocal)
            {
                candidateLocal = candidateLocal.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(candidateLocal, tz);
        }
    }
}
