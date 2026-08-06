using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Core.Helpers;
using PQM.Infrastructure.Services;
using PQM.Server.Hubs;

namespace PQM.Server.Services
{
   
    public class DeviceScheduleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceScheduleRunnerService> _logger;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly string _connectionString;

        public DeviceScheduleRunnerService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IHubContext<DeviceHub> hubContext,
            ILogger<DeviceScheduleRunnerService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DeviceScheduleRunnerService] Background Schedule Runner Started.");

            int tickCounter = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                tickCounter++;
                if (tickCounter % 12 == 1)
                {
                    _logger.LogInformation("[DeviceScheduleRunnerService] Heartbeat — Service active and checking schedules. Time: {TimeUtc:yyyy-MM-dd HH:mm:ss UTC}.", DateTime.UtcNow);
                }

                try
                {
                    await ProcessPendingSyncRequestsAsync(stoppingToken);
                    await ProcessDueSchedulesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DeviceScheduleRunnerService] Error during schedule execution cycle.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("[DeviceScheduleRunnerService] Background Schedule Runner Stopped.");
        }

        private async Task SendDeviceStatusChangedAsync(int deviceId, string status, string? lastSync, string? lastError)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
                {
                    deviceId,
                    status,
                    lastSync,
                    lastError
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DeviceScheduleRunnerService] Failed to send SignalR update to clients.");
            }
        }

        private async Task ProcessPendingSyncRequestsAsync(CancellationToken stoppingToken)
        {
            var pendingRequests = await GetPendingSyncRequestsAsync(stoppingToken);
            if (pendingRequests.Count == 0) return;

            foreach (var req in pendingRequests)
            {
                if (stoppingToken.IsCancellationRequested) break;

                using var scope = _scopeFactory.CreateScope();
                var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

                if (profileSyncService.IsDeviceSyncing(req.DeviceId))
                {
                    _logger.LogInformation("[DeviceScheduleRunnerService] Device {DeviceId} is already syncing. Skipping request {RequestId}.", req.DeviceId, req.Id);
                    continue;
                }

                await UpdateSyncRequestStatusAsync(req.Id, "Processing", null, stoppingToken);

                _logger.LogInformation("[DeviceScheduleRunnerService] Executing on-demand sync for Device {DeviceId} (Request #{RequestId})...", req.DeviceId, req.Id);

                await SendDeviceStatusChangedAsync(req.DeviceId, "Syncing", null, null);

                var result = await profileSyncService.SyncDeviceAllProfilesAsync(req.DeviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";
                string? finalError = result.ErrorMessage;

                await SendDeviceStatusChangedAsync(req.DeviceId, finalStatus, DateTime.UtcNow.ToString("o"), finalError);

                string reqFinalStatus = result.Success ? "Completed" : "Failed";
                await UpdateSyncRequestStatusAsync(req.Id, reqFinalStatus, finalError, stoppingToken);

                _logger.LogInformation("[DeviceScheduleRunnerService] Completed on-demand sync for Device {DeviceId}. Status={Status}", req.DeviceId, reqFinalStatus);
            }
        }

        private async Task ProcessDueSchedulesAsync(CancellationToken stoppingToken)
        {
            var dueSchedules = await GetDueSchedulesAsync(stoppingToken);
            if (dueSchedules.Count == 0) return;

            _logger.LogInformation("[DeviceScheduleRunnerService] Found {Count} due schedule(s) to execute.", dueSchedules.Count);

            foreach (var item in dueSchedules)
            {
                if (stoppingToken.IsCancellationRequested) break;

                using var scope = _scopeFactory.CreateScope();
                var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

                if (profileSyncService.IsDeviceSyncing(item.DeviceId))
                {
                    _logger.LogInformation("[DeviceScheduleRunnerService] Device {DeviceId} is already syncing. Skipping scheduled run for this tick.", item.DeviceId);
                    continue;
                }

                _logger.LogInformation("[DeviceScheduleRunnerService] Triggering scheduled sync for Device {DeviceId} (ScheduledTime: {ScheduledTime}, TimeZone: {TimeZoneId})...", item.DeviceId, item.ScheduledTime, item.TimeZoneId);

                await SendDeviceStatusChangedAsync(item.DeviceId, "Syncing", null, null);

                var result = await profileSyncService.SyncDeviceAllProfilesAsync(item.DeviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";
                string? finalError = result.ErrorMessage;

                await SendDeviceStatusChangedAsync(item.DeviceId, finalStatus, DateTime.UtcNow.ToString("o"), finalError);

                DateTime nowUtc = DateTime.UtcNow;
                DateTime? nextRunAtUtc = ScheduleHelper.ComputeNextRunAtUtc(item.ScheduledTime, item.TimeZoneId, nowUtc);
                string lastRunStatus = result.Success ? "Success" : "Failed";

                await UpdateScheduleCompletionAsync(item.DeviceId, nowUtc, lastRunStatus, nextRunAtUtc, stoppingToken);

                _logger.LogInformation("[DeviceScheduleRunnerService] Completed scheduled sync for Device {DeviceId}. Status={Status}, NextRunAtUtc={NextRunAtUtc:yyyy-MM-dd HH:mm:ss UTC}", item.DeviceId, lastRunStatus, nextRunAtUtc);
            }
        }

        private class PendingRequestItem
        {
            public long Id { get; set; }
            public int DeviceId { get; set; }
        }

        private async Task<List<PendingRequestItem>> GetPendingSyncRequestsAsync(CancellationToken cancellationToken)
        {
            var list = new List<PendingRequestItem>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TOP 10 Id, DeviceId
                FROM DeviceSyncRequest
                WHERE Status = 'Pending'
                ORDER BY RequestedAt ASC";

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new PendingRequestItem
                {
                    Id = reader.GetInt64(0),
                    DeviceId = reader.GetInt32(1)
                });
            }

            return list;
        }

        private async Task UpdateSyncRequestStatusAsync(long requestId, string status, string? errorMessage, CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DeviceSyncRequest
                SET Status = @status,
                    ErrorMessage = @errorMessage
                WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
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
    }
}
