using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PQM.Console.Options;
using PQM.Infrastructure.Services;
using PQM.Core.Helpers;

namespace PQM.Console
{
    public class DeviceConsoleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeviceConsoleRunnerService> _logger;
        private readonly ConsoleOptions _options;
        private readonly string _connectionString;
        private readonly string _serverHubUrl;
        private HubConnection? _hubConnection;

        public DeviceConsoleRunnerService(IServiceScopeFactory scopeFactory,IOptions<ConsoleOptions> options,ILogger<DeviceConsoleRunnerService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _connectionString = !string.IsNullOrWhiteSpace(_options.DefaultConnection)? _options.DefaultConnection: throw new InvalidOperationException("Connection string 'DefaultConnection' not found in options.");

            _serverHubUrl = _options.ServerHubUrl;
        }

        //private static Mutex? _singleInstanceMutex;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //bool createdNew = false;
            //try
            //{
            //    _singleInstanceMutex = new Mutex(true, @"Global\PQMMeterReader_SingleInstance_Mutex", out createdNew);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning("[PQM.Console] Exception creating single-instance mutex: {Message}. Continuing with process check.", ex.Message);
            //}

            //if (!createdNew)
            //{
            //    System.Console.WriteLine("[PQM.Console] Another instance of PQMMeterReader / PQM.Console is already active. Exiting duplicate instance.");
            //    _logger.LogWarning("[PQM.Console] Another instance of PQMMeterReader / PQM.Console is already active. Exiting duplicate instance.");
            //    return;
            //}

            //System.Console.WriteLine($"[PQM.Console] Production Sync Runner Started. Target Hub: {_serverHubUrl}");
            //_logger.LogInformation("[PQM.Console] Production Sync Runner Started. Target Hub: {HubUrl}", _serverHubUrl);

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
                    //await ProcessPendingScanRequestsAsync(stoppingToken);
                    //await ProcessPendingSyncRequestsAsync(stoppingToken);
                    await ProcessDueSchedulesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PQM.Console] Error during sync execution cycle: {Message}", ex.Message);
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

            // Process sync requests concurrently across different devices.
            // ProfileSyncService.TryAcquireLock ensures requests for the SAME device are safely serialized.
            var tasks = pendingRequests.Select(async req =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                if (!ProfileSyncService.TryAcquireLock(req.DeviceId))
                {
                    _logger.LogInformation(
                        "[PQM.Console] Device {DeviceId} is already syncing. Skipping request {RequestId}.",
                        req.DeviceId, req.Id);
                    return;
                }

                using var statusCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                try
                {
                    // Mark request as 'Processing'
                    await UpdateSyncRequestStatusAsync(req.Id, "Processing", null, statusCts.Token);

                    _logger.LogInformation("[PQM.Console] Executing on-demand sync for Device {DeviceId} (Request #{RequestId})...", req.DeviceId, req.Id);

                    // 1. Broadcast SignalR "Syncing"
                    await SendDeviceStatusChangedAsync(req.DeviceId, "Syncing", null, null);

                    // 2. Execute Sync
                    using var scope = _scopeFactory.CreateScope();
                    var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();
                    var result = await profileSyncService.SyncDeviceAllProfilesAsync(req.DeviceId, stoppingToken);

                    string finalStatus = result.Success ? "Online" : "Error";
                    string? finalError = result.ErrorMessage;

                    // 3. Broadcast SignalR completion state
                    await SendDeviceStatusChangedAsync(req.DeviceId, finalStatus, DateTime.UtcNow.ToString("o"), finalError);

                    // 4. Update request completion status
                    string reqFinalStatus = result.Success ? "Completed" : "Failed";
                    using var completeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await UpdateSyncRequestStatusAsync(req.Id, reqFinalStatus, finalError, completeCts.Token);

                    _logger.LogInformation(
                        "[PQM.Console] Completed on-demand sync for Device {DeviceId}. Status={Status}",
                        req.DeviceId, reqFinalStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PQM.Console] Exception in sync request {RequestId} for Device {DeviceId}: {Message}", req.Id, req.DeviceId, ex.Message);
                    using var failCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await UpdateSyncRequestStatusAsync(req.Id, "Failed", ex.Message, failCts.Token);
                }
                finally
                {
                    ProfileSyncService.ReleaseLock(req.DeviceId);
                }
            });

            await Task.WhenAll(tasks);
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

            if (dueSchedules.Count == 0)
                return;

            _logger.LogInformation(
                "[PQM.Console] Found {Count} due schedule(s) to execute.",
                dueSchedules.Count);

            foreach (var schedule in dueSchedules)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                if (schedule.DeviceIds.Count == 0)
                {
                    _logger.LogWarning(
                        "[PQM.Console] Schedule {ScheduleId} has no devices assigned.",
                        schedule.ScheduleId);

                    continue;
                }

                _logger.LogInformation(
                    "[PQM.Console] Executing Schedule {ScheduleId} for {DeviceCount} device(s).",
                    schedule.ScheduleId,
                    schedule.DeviceIds.Count);

                // Calculate next run ONCE for the schedule.
                DateTime nowUtc = DateTime.UtcNow;

                DateTime? nextRunAtUtc =
                    ScheduleHelper.ComputeNextRunAtUtc(
                        schedule.ScheduledTime,
                        schedule.TimeZoneId,
                        nowUtc);

                // Advance schedule immediately.
                using var advanceCts =
                    new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await UpdateScheduleCompletionAsync(
                    schedule.ScheduleId,
                    nowUtc,
                    "Running",
                    nextRunAtUtc,
                    advanceCts.Token);

                // Run all devices belonging to this schedule.
                var deviceTasks = schedule.DeviceIds.Select(
                    deviceId => ProcessScheduledDeviceAsync(
                        deviceId,
                        schedule.ScheduleId,
                        stoppingToken));

                await Task.WhenAll(deviceTasks);

                // Mark schedule execution completed.
                using var completionCts =
                    new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await UpdateScheduleCompletionAsync(
                    schedule.ScheduleId,
                    DateTime.UtcNow,
                    "Success",
                    nextRunAtUtc,
                    completionCts.Token);

                _logger.LogInformation(
                    "[PQM.Console] Completed Schedule {ScheduleId}. NextRun={NextRunAtUtc}",
                    schedule.ScheduleId,
                    nextRunAtUtc);
            }
        }

        private async Task ProcessScheduledDeviceAsync(int deviceId,int scheduleId,CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = _scopeFactory.CreateScope();

            var profileSyncService =
                scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

            // Prevent same device from syncing twice.
            if (!ProfileSyncService.TryAcquireLock(deviceId))
            {
                _logger.LogInformation(
                    "[PQM.Console] Device {DeviceId} is already syncing. " +
                    "Skipping scheduled run.",
                    deviceId);

                return;
            }

            try
            {
                _logger.LogInformation(
                    "[PQM.Console] Schedule {ScheduleId}: Starting sync for Device {DeviceId}.",
                    scheduleId,
                    deviceId);

                // 1. SignalR - Syncing
                await SendDeviceStatusChangedAsync(
                    deviceId,
                    "Syncing",
                    null,
                    null);

                // 2. Execute actual device sync
                var result =
                    await profileSyncService.SyncDeviceAllProfilesAsync(
                        deviceId,
                        stoppingToken);

                string finalStatus =
                    result.Success ? "Online" : "Error";

                string? finalError =
                    result.ErrorMessage;

                // 3. SignalR - completed
                await SendDeviceStatusChangedAsync(
                    deviceId,
                    finalStatus,
                    DateTime.UtcNow.ToString("o"),
                    finalError);

                _logger.LogInformation(
                    "[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} completed. Status={Status}",
                    scheduleId,
                    deviceId,
                    finalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} sync failed.",
                    scheduleId,
                    deviceId);

                await SendDeviceStatusChangedAsync(
                    deviceId,
                    "Error",
                    DateTime.UtcNow.ToString("o"),
                    ex.Message);
            }
            finally
            {
                ProfileSyncService.ReleaseLock(deviceId);
            }
        }

        private class DueScheduleItem
        {
            public int ScheduleId { get; set; }
            public TimeSpan ScheduledTime { get; set; }
            public string RepeatMode { get; set; } = "Daily";
            public string? TimeZoneId { get; set; }
            public List<int> DeviceIds { get; set; } = new();
        }

        private async Task<List<DueScheduleItem>> GetDueSchedulesAsync(CancellationToken cancellationToken)
        {
            var list = new List<DueScheduleItem>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
        SELECT 
            s.Id,
            s.ScheduledTime,
            s.RepeatMode,
            s.NextRunAtUtc,
            d.Id,
            d.TimeZoneId
        FROM DeviceSyncSchedule s
        INNER JOIN Devices d 
            ON d.DeviceSyncScheduleId = s.Id
        WHERE s.IsEnabled = 1
          AND s.NextRunAtUtc IS NOT NULL
          AND s.NextRunAtUtc <= @nowUtc
          AND (d.IsDeleted = 0 OR d.IsDeleted IS NULL)
        ORDER BY s.Id";

            cmd.Parameters.AddWithValue("@nowUtc", DateTime.UtcNow);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var schedules = new Dictionary<int, DueScheduleItem>();

            while (await reader.ReadAsync(cancellationToken))
            {
                int scheduleId = reader.GetInt32(0);

                if (!schedules.TryGetValue(scheduleId, out var schedule))
                {
                    schedule = new DueScheduleItem
                    {
                        ScheduleId = scheduleId,
                        ScheduledTime = reader.GetTimeSpan(1),
                        RepeatMode = reader.IsDBNull(2)
                            ? "Daily"
                            : reader.GetString(2)
                    };

                    schedules.Add(scheduleId, schedule);
                }

                int deviceId = reader.GetInt32(4);

                schedule.DeviceIds.Add(deviceId);

                // Use device timezone.
                if (string.IsNullOrWhiteSpace(schedule.TimeZoneId) &&
                    !reader.IsDBNull(5))
                {
                    schedule.TimeZoneId = reader.GetString(5);
                }
            }

            return schedules.Values.ToList();
        }

        private async Task UpdateScheduleCompletionAsync(int scheduleId,DateTime lastRunAtUtc,string lastRunStatus,DateTime? nextRunAtUtc,CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync(cancellationToken);

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
        UPDATE DeviceSyncSchedule
        SET LastRunAtUtc = @lastRunAtUtc,
            LastRunStatus = @lastRunStatus,
            NextRunAtUtc = @nextRunAtUtc
        WHERE Id = @scheduleId";

            cmd.Parameters.AddWithValue("@scheduleId", scheduleId);
            cmd.Parameters.AddWithValue("@lastRunAtUtc", lastRunAtUtc);
            cmd.Parameters.AddWithValue("@lastRunStatus", lastRunStatus);
            cmd.Parameters.AddWithValue(
                "@nextRunAtUtc",
                (object?)nextRunAtUtc ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private class PendingScanItem
        {
            public long Id { get; set; }
            public int DeviceId { get; set; }
            public int? ProfileId { get; set; }
            public string? ParameterIdsJson { get; set; }
        }

        private async Task ProcessPendingScanRequestsAsync(CancellationToken stoppingToken)
        {
            var pendingScans = await GetPendingScanRequestsAsync(stoppingToken);
            if (pendingScans.Count == 0) return;

            // Process scan requests concurrently across different devices.
            // ProfileSyncService.TryAcquireLock ensures scans for the SAME device are serialized,
            // while scans for DIFFERENT devices run in parallel.
            var tasks = pendingScans.Select(async scan =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                // Use the same lock as scheduled syncs — scan and sync are mutually exclusive per device
                if (!ProfileSyncService.TryAcquireLock(scan.DeviceId))
                {
                    _logger.LogInformation("[PQM.Console] Device {DeviceId} is already syncing/scanning. Scan request {ScanId} will retry next tick.", scan.DeviceId, scan.Id);
                    return;
                }

                using var procCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await UpdateScanRequestStatusAsync(scan.Id, "Processing", null, null, procCts.Token);
                _logger.LogInformation("[PQM.Console] Executing live scan for Device {DeviceId} (ScanRequest #{ScanId})...", scan.DeviceId, scan.Id);

                try
                {
                    var result = await ExecuteScanAsync(scan, stoppingToken);
                    string resultJson = JsonSerializer.Serialize(result);
                    using var compCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await UpdateScanRequestStatusAsync(scan.Id, "Completed", resultJson, null, compCts.Token);
                    _logger.LogInformation("[PQM.Console] Completed live scan for Device {DeviceId} (ScanRequest #{ScanId}). Items={Count}", scan.DeviceId, scan.Id, (result.Items?.Count ?? 0));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PQM.Console] Live scan failed for Device {DeviceId} (ScanRequest #{ScanId}).", scan.DeviceId, scan.Id);
                    using var failCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await UpdateScanRequestStatusAsync(scan.Id, "Failed", null, ex.Message, failCts.Token);
                }
                finally
                {
                    ProfileSyncService.ReleaseLock(scan.DeviceId);
                }
            });

            await Task.WhenAll(tasks);
        }

        private class ScanResultPayload
        {
            public string ScannedAt { get; set; } = string.Empty;
            public int DeviceId { get; set; }
            public string? DeviceName { get; set; }
            public List<ScanResultItem> Items { get; set; } = new();
        }

        private class ScanResultItem
        {
            public int ParameterId { get; set; }
            public string ParameterName { get; set; } = string.Empty;
            public string? ObisCode { get; set; }
            public string Value { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public string? Error { get; set; }
        }

        private async Task<PQM.Core.Entities.Device?> LoadDeviceAsync(int deviceId, CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, IP, PORT, ClientAddress, ServerAddress, AuthenticationTypeId, Password, Timeout, TimeZoneId 
                FROM Devices 
                WHERE Id = @id AND (IsDeleted = 0 OR IsDeleted IS NULL)";
            cmd.Parameters.AddWithValue("@id", deviceId);
            using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken))
                return null;

            return new PQM.Core.Entities.Device
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                IP = r.IsDBNull(2) ? " " : r.GetString(2),
                PORT = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                ClientAddress = r.IsDBNull(4) ? 16 : r.GetInt32(4),
                ServerAddress = r.IsDBNull(5) ? 1 : r.GetInt32(5),
                AuthenticationTypeId = r.IsDBNull(6) ? null : r.GetInt32(6),
                Password = r.IsDBNull(7) ? null : r.GetString(7),
                Timeout = r.IsDBNull(8) ? 30000 : r.GetInt32(8),
                TimeZoneId = r.IsDBNull(9) ? null : r.GetString(9)
            };
        }

        private async Task<ScanResultPayload> ExecuteScanAsync(PendingScanItem scan, CancellationToken stoppingToken)
        {
            // Load device via shared helper
            PQM.Core.Entities.Device device = await LoadDeviceAsync(scan.DeviceId, stoppingToken)
                ?? throw new InvalidOperationException($"Device {scan.DeviceId} not found.");

            // Load parameters to scan
            List<int>? paramIds = [];
            if (!string.IsNullOrWhiteSpace(scan.ParameterIdsJson))
            {
                paramIds = JsonSerializer.Deserialize<List<int>>(scan.ParameterIdsJson) ?? [];
            }
            var parametersToRead = new List<(int Id, string Name, string? ObisCode, string? ObjectType, int AttrIdx, int? Scaler, string? Unit)>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync(stoppingToken);
                using var cmd = conn.CreateCommand();
                var sql = "SELECT Id, Name, ObisCode, ObjectType, AttributeIndex, Scaler, Unit FROM Parameters WHERE IsVisible = 1";
                if (scan.ProfileId.HasValue && scan.ProfileId.Value > 0)
                    sql += " AND ProfileId = @profileId";
                if (paramIds != null && paramIds.Count > 0)
                {
                    var inClause = string.Join(",", paramIds.Select((_, i) => $"@p{i}"));
                    sql += $" AND Id IN ({inClause})";
                }
                cmd.CommandText = sql;
                if (scan.ProfileId.HasValue && scan.ProfileId.Value > 0)
                    cmd.Parameters.AddWithValue("@profileId", scan.ProfileId.Value);
                if (paramIds != null)
                    for (int i = 0; i < paramIds.Count; i++)
                        cmd.Parameters.AddWithValue($"@p{i}", paramIds[i]);

                using var r = await cmd.ExecuteReaderAsync(stoppingToken);
                while (await r.ReadAsync(stoppingToken))
                    parametersToRead.Add((
                        Id: r.GetInt32(0),
                        Name: r.GetString(1),
                        ObisCode: r.IsDBNull(2) ? null : r.GetString(2),
                        ObjectType: r.IsDBNull(3) ? null : r.GetString(3),
                        AttrIdx: r.IsDBNull(4) ? 2 : r.GetInt32(4),
                        Scaler: r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                        Unit: r.IsDBNull(6) ? null : r.GetString(6)
                    ));
            }

            var items = new List<ScanResultItem>();

            if (parametersToRead.Count > 0)
            {
                using var hardCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                hardCts.CancelAfter(TimeSpan.FromMinutes(5));
                var scanToken = hardCts.Token;

                await using var reader = new DlmsMeterReader(device, verboseLogging: false);
                await reader.ConnectAsync(scanToken);
                await reader.ReadAssociationViewAsync(scanToken);

                foreach (var (pId, pName, obisCode, objectType, attrIdx, scaler, unit) in parametersToRead)
                {
                    if (scanToken.IsCancellationRequested) break;
                    if (string.IsNullOrWhiteSpace(obisCode)) continue;

                    using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(scanToken);
                    itemCts.CancelAfter(TimeSpan.FromMilliseconds(2500));

                    try
                    {
                        var meterObj = reader.FindObjectByObis(obisCode);
                        if (meterObj == null)
                        {
                            meterObj = Enum.TryParse<Gurux.DLMS.Enums.ObjectType>(objectType, true, out var ot)
                                ? Gurux.DLMS.GXDLMSClient.CreateObject(ot)
                                : Gurux.DLMS.GXDLMSClient.CreateObject(Gurux.DLMS.Enums.ObjectType.Register);
                            meterObj.LogicalName = obisCode;
                        }

                        object? rawValue = await reader.ReadObjectAsync(meterObj, attrIdx, itemCts.Token);

                        string formattedValue = string.Empty;
                        if (rawValue != null)
                        {
                            if (scaler.HasValue && scaler.Value != 0 &&
                                (rawValue is sbyte || rawValue is short || rawValue is int || rawValue is long ||
                                 rawValue is float || rawValue is double || rawValue is decimal))
                            {
                                double numVal = Convert.ToDouble(rawValue);
                                formattedValue = Math.Round(numVal * Math.Pow(10, scaler.Value), 4)
                                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                formattedValue = ValueFormatter.CleanValue(ValueFormatter.FormatValue(rawValue));
                            }
                        }

                        items.Add(new ScanResultItem { ParameterId = pId, ParameterName = pName, ObisCode = obisCode, Value = formattedValue, Unit = unit });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[PQM.Console] Scan param '{Name}' ({ObisCode}) failed for Device {DeviceId}: {Error}", pName, obisCode, scan.DeviceId, ex.Message);
                        items.Add(new ScanResultItem { ParameterId = pId, ParameterName = pName, ObisCode = obisCode, Value = "N/A", Unit = unit, Error = ex.Message });
                    }
                }
            }

            return new ScanResultPayload
            {
                ScannedAt = DateTime.UtcNow.ToString("o"),
                DeviceId = scan.DeviceId,
                DeviceName = device.Name,
                Items = items
            };
        }

        private async Task<List<PendingScanItem>> GetPendingScanRequestsAsync(CancellationToken cancellationToken)
        {
            var list = new List<PendingScanItem>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TOP 5 Id, DeviceId, ProfileId, ParameterIds
                FROM DeviceScanRequest
                WHERE Status = 'Pending'
                ORDER BY RequestedAt ASC";
            using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
                list.Add(new PendingScanItem
                {
                    Id = r.GetInt64(0),
                    DeviceId = r.GetInt32(1),
                    ProfileId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                    ParameterIdsJson = r.IsDBNull(3) ? null : r.GetString(3)
                });
            return list;
        }

        private async Task UpdateScanRequestStatusAsync(long id, string status, string? resultJson, string? errorMessage, CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DeviceScanRequest
                SET Status = @status,
                    ResultJson = @resultJson,
                    ErrorMessage = @errorMessage,
                    CompletedAt = CASE WHEN @status IN ('Completed','Failed') THEN GETUTCDATE() ELSE NULL END
                WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@resultJson", (object?)resultJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
