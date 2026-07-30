using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure.Services;
using PQM.Core.Helpers;

namespace PQM.Console
{
    public class DeviceConsoleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceConsoleRunnerService> _logger;
        private readonly string _connectionString;
        private readonly string _serverHubUrl;
        private HubConnection? _hubConnection;

        public DeviceConsoleRunnerService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<DeviceConsoleRunnerService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            _serverHubUrl = configuration["ServerHubUrl"] ?? "http://localhost:5135/hubs/device";
        }

        private static Mutex? _singleInstanceMutex;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool createdNew = false;
            try
            {
                _singleInstanceMutex = new Mutex(true, @"Global\PQMMeterReader_SingleInstance_Mutex", out createdNew);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PQM.Console] Exception creating single-instance mutex: {Message}. Continuing with process check.", ex.Message);
            }

            if (!createdNew)
            {
                System.Console.WriteLine("[PQM.Console] Another instance of PQMMeterReader / PQM.Console is already active. Exiting duplicate instance.");
                _logger.LogWarning("[PQM.Console] Another instance of PQMMeterReader / PQM.Console is already active. Exiting duplicate instance.");
                return;
            }

            System.Console.WriteLine($"[PQM.Console] Production Sync Runner Started. Target Hub: {_serverHubUrl}");
            _logger.LogInformation("[PQM.Console] Production Sync Runner Started. Target Hub: {HubUrl}", _serverHubUrl);

            // Initialize SignalR Hub Connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverHubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "[PQM.Console] SignalR connection closed. Reconnecting automatically when possible.");
                await Task.CompletedTask;
            };

            // Attempt initial connection to SignalR hub in background (non-blocking if server is down)
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_hubConnection.State == HubConnectionState.Disconnected)
                        {
                            await _hubConnection.StartAsync(stoppingToken);
                            System.Console.WriteLine("[PQM.Console] Connected to PQM.Server SignalR Hub.");
                            _logger.LogInformation("[PQM.Console] Connected to PQM.Server SignalR Hub.");
                        }
                        break;
                    }
                    catch
                    {
                        // Retry connection after 5 seconds if server is not up yet
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }, stoppingToken);

            int tickCounter = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                tickCounter++;
                if (tickCounter % 12 == 1) 
                {
                    _logger.LogInformation("[PQM.Console] Service Heartbeat — Service active and polling. Time: {TimeUtc:yyyy-MM-dd HH:mm:ss UTC}.", DateTime.UtcNow);
                }

                try
                {
                    await ProcessPendingSyncRequestsAsync(stoppingToken);

                    await ProcessDueSchedulesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PQM.Console] Error during sync execution cycle.");
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

            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync(CancellationToken.None);
                await _hubConnection.DisposeAsync();
            }

            System.Console.WriteLine("[PQM.Console] Production Sync Runner Stopped.");
            _logger.LogInformation("[PQM.Console] Production Sync Runner Stopped.");
        }

        private async Task SendDeviceStatusChangedAsync(int deviceId, string status, string? lastSync, string? lastError)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastDeviceStatus", deviceId, status, lastSync, lastError);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PQM.Console] Failed to send SignalR update to PQM.Server.");
                }
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
                    _logger.LogInformation(
                        "[PQM.Console] Device {DeviceId} is already syncing. Skipping request {RequestId}.",
                        req.DeviceId, req.Id);
                    continue;
                }

                // Mark request as 'Processing'
                await UpdateSyncRequestStatusAsync(req.Id, "Processing", null, stoppingToken);

                _logger.LogInformation("[PQM.Console] Executing on-demand sync for Device {DeviceId} (Request #{RequestId})...", req.DeviceId, req.Id);
                System.Console.WriteLine($"[PQM.Console] Executing on-demand sync for Device {req.DeviceId} (Request #{req.Id})...");

                // 1. Broadcast SignalR "Syncing"
                await SendDeviceStatusChangedAsync(req.DeviceId, "Syncing", null, null);

                // 2. Execute Sync
                var result = await profileSyncService.SyncDeviceAllProfilesAsync(req.DeviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";
                string? finalError = result.ErrorMessage;

                // 3. Broadcast SignalR completion state
                await SendDeviceStatusChangedAsync(req.DeviceId, finalStatus, DateTime.UtcNow.ToString("o"), finalError);

                // 4. Update request completion status
                string reqFinalStatus = result.Success ? "Completed" : "Failed";
                await UpdateSyncRequestStatusAsync(req.Id, reqFinalStatus, finalError, stoppingToken);

                _logger.LogInformation(
                    "[PQM.Console] Completed on-demand sync for Device {DeviceId}. Status={Status}",
                    req.DeviceId, reqFinalStatus);
                System.Console.WriteLine($"[PQM.Console] Completed on-demand sync for Device {req.DeviceId}. Status={reqFinalStatus}");
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

        private async Task ProcessDueSchedulesAsync(CancellationToken stoppingToken)
        {
            var dueSchedules = await GetDueSchedulesAsync(stoppingToken);
            if (dueSchedules.Count == 0) return;

            _logger.LogInformation("[PQM.Console] Found {Count} due schedule(s) to execute.", dueSchedules.Count);

            foreach (var item in dueSchedules)
            {
                if (stoppingToken.IsCancellationRequested) break;

                using var scope = _scopeFactory.CreateScope();
                var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

                if (profileSyncService.IsDeviceSyncing(item.DeviceId))
                {
                    _logger.LogInformation(
                        "[PQM.Console] Device {DeviceId} is already syncing. Skipping scheduled run for this tick.",
                        item.DeviceId);
                    continue;
                }

                _logger.LogInformation(
                    "[PQM.Console] Triggering scheduled sync for Device {DeviceId} (ScheduledTime: {ScheduledTime}, TimeZone: {TimeZoneId})...",
                    item.DeviceId, item.ScheduledTime, item.TimeZoneId);
                System.Console.WriteLine($"[PQM.Console] Triggering scheduled sync for Device {item.DeviceId}...");

                // 1. Broadcast SignalR "Syncing"
                await SendDeviceStatusChangedAsync(item.DeviceId, "Syncing", null, null);

                // 2. Execute Sync
                var result = await profileSyncService.SyncDeviceAllProfilesAsync(item.DeviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";
                string? finalError = result.ErrorMessage;

                // 3. Broadcast SignalR completion state
                await SendDeviceStatusChangedAsync(item.DeviceId, finalStatus, DateTime.UtcNow.ToString("o"), finalError);

                // 4. Update DeviceSyncSchedule record
                DateTime nowUtc = DateTime.UtcNow;
                DateTime? nextRunAtUtc = ScheduleHelper.ComputeNextRunAtUtc(item.ScheduledTime, item.TimeZoneId, nowUtc);
                string lastRunStatus = result.Success ? "Success" : "Failed";

                await UpdateScheduleCompletionAsync(item.DeviceId, nowUtc, lastRunStatus, nextRunAtUtc, stoppingToken);

                _logger.LogInformation(
                    "[PQM.Console] Completed scheduled sync for Device {DeviceId}. Status={Status}, NextRunAtUtc={NextRunAtUtc:yyyy-MM-dd HH:mm:ss UTC}",
                    item.DeviceId, lastRunStatus, nextRunAtUtc);
                System.Console.WriteLine($"[PQM.Console] Completed scheduled sync for Device {item.DeviceId}. Status={lastRunStatus}");
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
    }
}
