using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PQM.Core.DTOs;
using PQM.Core.Entities;
using PQM.Core.Helpers;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure;
using PQM.Infrastructure.Repositories;
using PQM.Infrastructure.Services;
using System.Net.Http;

namespace PQM.Console
{
    public class DeviceConsoleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeviceConsoleRunnerService> _logger;
        private readonly ConsoleOptions _options;
        private readonly IConfiguration _configuration;
        public DeviceConsoleRunnerService(IServiceScopeFactory scopeFactory,IOptions<ConsoleOptions> options,ILogger<DeviceConsoleRunnerService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
            _configuration = configuration;
        }
        private async Task CreateScheduleNotificationAsync(SyncRunLogs runLog,CancellationToken cancellationToken)
        {
            var duration = TimeSpan.FromMilliseconds(runLog.DurationMs ?? 0);

            var durationText = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";

            var notification = new Notification
            {
                Title = "Schedule Completed",
                Message = $"Schedule completed. {runLog.SucceededDevices} of {runLog.TotalDevices} devices succeeded, " +
                          $"{runLog.FailedDevices} failed. Duration: {durationText}.",
                Type = "Schedule",
                Severity = runLog.Status == "Success"
                    ? "Success"
                    : runLog.Status == "PartialSuccess"
                        ? "Warning"
                        : "Error"
            };

            using var scope = _scopeFactory.CreateScope();

            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var result = await notificationRepository.CreateAsync(notification,cancellationToken);

            var baseUrl = _configuration["ServerApi:BaseUrl"] ?? throw new InvalidOperationException("ServerApi:BaseUrl is missing from appsettings.json.");

            using var httpClient = new HttpClient();

            var url = $"{baseUrl.TrimEnd('/')}/api/notification/Dispatch/{result.Id}";

            var response = await httpClient.PostAsync(url,null,cancellationToken);

            response.EnsureSuccessStatusCode();

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[PQM] Service started | Checking schedules every 5 seconds.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueSchedulesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[PQM] Error during sync execution cycle: {Message}",
                        ex.Message);
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "[PQM] Service stopped.");
        }
        private async Task ProcessDueSchedulesAsync(CancellationToken stoppingToken)
        {
            var dueSchedules = await GetDueSchedulesAsync(stoppingToken);

            if (dueSchedules.Count == 0)
                return;
            _logger.LogInformation("[PQM] Found {Count} schedule(s) ready to run.",dueSchedules.Count);

            foreach (var schedule in dueSchedules)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;
                _logger.LogInformation("[PQM] Schedule {ScheduleId} started | Devices: {DeviceCount}",
                    schedule.ScheduleId,
                    schedule.DeviceIds.Count);

                DateTime nowIST = GetIndiaStandardTime();

                DateTime? nextRunAtIST =
                    ScheduleHelper.ComputeNextRunAt(
                        schedule.ScheduledTime,
                        nowIST);

                using var scope = _scopeFactory.CreateScope();

                var syncRunLogRepository =
                    scope.ServiceProvider
                        .GetRequiredService<ISyncRunLogRepository>();

                var runLog = new SyncRunLogs
                {
                    ScheduleId = schedule.ScheduleId,
                    StartedAt = nowIST,
                    Status = SyncRunStatus.Running,
                    TotalDevices = schedule.DeviceIds.Count,
                    NextRunAt = nextRunAtIST
                };

                runLog = await syncRunLogRepository
                    .CreateScheduleRunAsync(runLog);

                int runId = runLog.Id;

                using (var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(5)))
                {
                    await UpdateScheduleCompletionAsync(
                        schedule.ScheduleId,
                        nowIST,
                        "Running",
                        nextRunAtIST,
                        cts.Token);
                }

                string finalStatus;
                int succeeded = 0;
                int total = schedule.DeviceIds.Count;

                try
                {
                    var tasks = schedule.DeviceIds.Select(deviceId =>
                        ProcessScheduledDeviceAsync(
                            deviceId,
                            schedule.ScheduleId,
                            runId,
                            stoppingToken));

                    var outcomes = await Task.WhenAll(tasks);

                    succeeded = outcomes.Count(x => x.Success);
                    int failed = total - succeeded;

                    finalStatus =
                        succeeded == total
                            ? SyncRunStatus.Success
                            : succeeded > 0
                                ? SyncRunStatus.PartialSuccess
                                : SyncRunStatus.Failed;

                    var errorMessages = outcomes
                        .Where(x =>
                            !x.Success &&
                            !string.IsNullOrWhiteSpace(x.ErrorMessage))
                        .GroupBy(x => x.ErrorMessage!)
                        .Select(group =>
                            $"Devices {string.Join(", ", group.Select(x => x.DeviceId))}: {group.Key}")
                        .ToList();

                    runLog.ErrorMessage = errorMessages.Count > 0
                        ? string.Join("; ", errorMessages)
                        : null;

                    runLog.CompletedAt = GetIndiaStandardTime();

                    runLog.DurationMs =
                        (long)(
                            runLog.CompletedAt.Value -
                            runLog.StartedAt).TotalMilliseconds;

                    runLog.SucceededDevices = succeeded;
                    runLog.FailedDevices = failed;
                    runLog.Status = finalStatus;
                    runLog.NextRunAt = nextRunAtIST;

                    await syncRunLogRepository.UpdateScheduleRunAsync(runLog);

                    await CreateScheduleNotificationAsync(runLog, stoppingToken);


                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"[PQM] Schedule {ScheduleId} | ERROR | Execution failed",schedule.ScheduleId);

                    finalStatus = SyncRunStatus.Failed;
                    runLog.CompletedAt = GetIndiaStandardTime();

                    runLog.DurationMs =
                        (long)(
                            runLog.CompletedAt.Value -
                            runLog.StartedAt).TotalMilliseconds;

                    runLog.SucceededDevices = succeeded;
                    runLog.FailedDevices = total - succeeded;
                    runLog.Status = SyncRunStatus.Failed;
                    runLog.ErrorMessage = ex.Message;
                    runLog.NextRunAt = nextRunAtIST;

                    await syncRunLogRepository
                        .UpdateScheduleRunAsync(runLog);
                }

                using (var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(5)))
                {
                    await UpdateScheduleCompletionAsync(
                        schedule.ScheduleId,
                        GetIndiaStandardTime(),
                        finalStatus,
                        nextRunAtIST,
                        cts.Token);
                }

                _logger.LogInformation(
                    "[PQM] Schedule {ScheduleId} completed | {Status} | Success: {Succeeded} | Failed: {Failed} | Next run: {NextRunAtIST}",
                    schedule.ScheduleId,
                    finalStatus,
                    succeeded,
                    total - succeeded,
                    nextRunAtIST);
            }
        }
        private async Task<(int DeviceId, bool Success, string? ErrorMessage)>ProcessScheduledDeviceAsync(int deviceId,int scheduleId,int runId,CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return (
                    deviceId,
                    false,
                    "Sync operation was cancelled.");
            }

            using var scope = _scopeFactory.CreateScope();

            var profileSyncService =
                scope.ServiceProvider
                    .GetRequiredService<ProfileSyncService>();

            var deviceRepository =
                scope.ServiceProvider
                    .GetRequiredService<IDeviceRepository>();

            var reachability =
                scope.ServiceProvider
                    .GetRequiredService<INetworkReachabilityService>();

            var syncRunLogRepository =
                scope.ServiceProvider
                    .GetRequiredService<ISyncRunLogRepository>();

            SyncDeviceRunLogs? deviceRunLog = null;

            try
            {
                var device = await deviceRepository
                    .GetByIdAsync(deviceId, stoppingToken);

                if (device == null)
                {
                    _logger.LogWarning("[PQM] Device {DeviceId} | NOT FOUND",deviceId);

                    return (
                        deviceId,
                        false,
                        $"Device {deviceId} not found.");
                }

                deviceRunLog = new SyncDeviceRunLogs
                {
                    RunId = runId,
                    DeviceId = device.Id,
                    IP = device.IP,
                    Port = device.PORT,
                    StartedAt = GetIndiaStandardTime(),
                    Outcome = "Running"
                };

                deviceRunLog = await syncRunLogRepository
                    .CreateDeviceRunAsync(deviceRunLog);

                _logger.LogInformation(
                    "[PQM] Device {DeviceId} | Checking connection...",
                    deviceId);

                bool reachable = await reachability.IsReachableAsync(
                    device.IP,
                    device.PORT,
                    5000,
                    stoppingToken);

                if (!reachable)
                {
                    _logger.LogWarning(
                        "[PQM] Device {DeviceId} | UNREACHABLE | {IP}:{PORT}",
                        deviceId,
                        device.IP,
                        device.PORT);

                    var completedAt = GetIndiaStandardTime();

                    deviceRunLog.CompletedAt = completedAt;
                    deviceRunLog.DurationMs =
                        (long)(
                            completedAt -
                            deviceRunLog.StartedAt).TotalMilliseconds;

                    deviceRunLog.Outcome = "Failed";
                    deviceRunLog.ErrorMessage = "Device is unreachable.";
                    deviceRunLog.ExceptionDetails =
                        "Reachability check returned false. No exception was thrown.";

                    await syncRunLogRepository
                        .UpdateDeviceRunAsync(deviceRunLog);

                    return (
                        deviceId,
                        false,
                        "Device is unreachable.");
                }

                _logger.LogInformation(
                    "[PQM] Device {DeviceId} | Sync started",
                    deviceId);

                var result = await profileSyncService
                    .SyncDeviceAllProfilesAsync(
                        deviceId,
                        deviceRunLog,
                        stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[PQM] Device {DeviceId} | Sync completed | Profiles: {Succeeded}/{Attempted}",
                        deviceId,
                        result.ProfilesSucceeded,
                        result.ProfilesAttempted);
                }
                else
                {
                    _logger.LogWarning(
                        "[PQM] Device {DeviceId} | FAILED | {Error}",
                        deviceId,
                        result.ErrorMessage);
                }

                return (
                    deviceId,
                    result.Success,
                    result.Success
                        ? null
                        : result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"[PQM] Device {DeviceId} | ERROR | Sync exception",deviceId);

                if (deviceRunLog != null)
                {
                    var completedAt = GetIndiaStandardTime();

                    deviceRunLog.CompletedAt = completedAt;
                    deviceRunLog.DurationMs =
                        (long)(
                            completedAt -
                            deviceRunLog.StartedAt).TotalMilliseconds;

                    deviceRunLog.Outcome = "Failed";
                    deviceRunLog.ErrorMessage = ex.Message;
                    deviceRunLog.ExceptionDetails = ex.ToString();

                    await syncRunLogRepository
                        .UpdateDeviceRunAsync(deviceRunLog);
                }

                return (
                    deviceId,
                    false,
                    ex.Message);
            }
        }
        private async Task<List<DueScheduleItem>>GetDueSchedulesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<DataContext>();

            var nowIST = GetIndiaStandardTime();

            var list = await db.DeviceSyncSchedules
                .AsNoTracking()
                .Where(s =>
                    s.IsEnabled &&
                    s.NextRunAt != null &&
                    s.NextRunAt <= nowIST)
                .OrderBy(s => s.Id)
                .Select(s => new DueScheduleItem
                {
                    ScheduleId = s.Id,
                    ScheduledTime = s.ScheduledTime,
                    RepeatMode = s.RepeatMode ?? "Daily",
                    TimeZoneId = "India Standard Time"
                })
                .ToListAsync(cancellationToken);

            if (list.Count == 0)
                return list;

            foreach (var schedule in list)
            {
                var deviceIds = await db.Device
                    .AsNoTracking()
                    .Where(d =>
                        d.IsActive &&
                        !d.IsDeleted &&
                        d.DeviceSyncScheduleId == schedule.ScheduleId)
                    .OrderBy(d => d.Id)
                    .Select(d => d.Id)
                    .ToListAsync(cancellationToken);

                schedule.DeviceIds.AddRange(deviceIds);
            }

            return list;
        }
        private async Task UpdateScheduleCompletionAsync(int scheduleId,DateTime lastRunAtIST,string lastRunStatus,DateTime? nextRunAtIST,CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<DataContext>();

            await db.DeviceSyncSchedules
                .Where(s => s.Id == scheduleId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(s => s.LastRunAt, lastRunAtIST)
                        .SetProperty(s => s.LastRunStatus, lastRunStatus)
                        .SetProperty(s => s.NextRunAt, nextRunAtIST),
                    cancellationToken);
        }
        private static DateTime GetIndiaStandardTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(
                    "India Standard Time"));
        }
    }
}