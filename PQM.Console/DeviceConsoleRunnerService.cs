using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PQM.Core.DTOs;
using PQM.Core.Helpers;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;

namespace PQM.Console
{
    public class DeviceConsoleRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeviceConsoleRunnerService> _logger;
        private readonly ConsoleOptions _options;
        public DeviceConsoleRunnerService(IServiceScopeFactory scopeFactory, IOptions<ConsoleOptions> options, ILogger<DeviceConsoleRunnerService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.DefaultConnection))
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in options.");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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

            _logger.LogInformation("[PQM.Console] Production Sync Runner Stopped.");
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

                _logger.LogInformation("[PQM.Console] Executing Schedule {ScheduleId} for {DeviceCount} device(s).",schedule.ScheduleId,schedule.DeviceIds.Count);

                DateTime nowUtc = DateTime.UtcNow;

                DateTime? nextRunAtUtc = ScheduleHelper.ComputeNextRunAtUtc(schedule.ScheduledTime, schedule.TimeZoneId, nowUtc);

                // Mark as Running immediately, and advance NextRunAtUtc now so this
                // schedule isn't picked up again on the next 5-second poll.
                using (var advanceCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await UpdateScheduleCompletionAsync(schedule.ScheduleId, nowUtc, "Running", nextRunAtUtc, advanceCts.Token);
                }

                string finalStatus;
                int succeeded = 0;
                int total = schedule.DeviceIds.Count;

                try
                {
                    var deviceTasks = schedule.DeviceIds.Select(deviceId => ProcessScheduledDeviceAsync(deviceId, schedule.ScheduleId, stoppingToken));

                    bool[] deviceOutcomes = await Task.WhenAll(deviceTasks);

                    succeeded = deviceOutcomes.Count(ok => ok);

                    // Schedule ran on time and completed → Success, regardless of individual device results.
                    finalStatus = "Success";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"[PQM.Console] Schedule {ScheduleId}: failed to execute.",schedule.ScheduleId);

                    finalStatus = "Failed";
                }

                using (var completionCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await UpdateScheduleCompletionAsync(schedule.ScheduleId, DateTime.UtcNow, finalStatus, nextRunAtUtc, completionCts.Token);
                }

                _logger.LogInformation("[PQM.Console] Completed Schedule {ScheduleId}. Status={Status} ({Succeeded}/{Total} devices). NextRun={NextRunAtUtc}",schedule.ScheduleId,finalStatus,succeeded,total,nextRunAtUtc);
            }
        }
        private async Task<bool> ProcessScheduledDeviceAsync(int deviceId, int scheduleId, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return false;

            using var scope = _scopeFactory.CreateScope();
            var profileSyncService = scope.ServiceProvider.GetRequiredService<ProfileSyncService>();

            // ✅ ADD THESE TWO LINES
            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var reachability = scope.ServiceProvider.GetRequiredService<INetworkReachabilityService>();

            try
            {
                // ✅ ADD THIS ENTIRE BLOCK
                var device = await deviceRepository.GetByIdAsync(deviceId, stoppingToken);
                if (device == null)
                {
                    _logger.LogWarning("[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} not found.", scheduleId, deviceId);
                    return false;
                }

                var reachable = await reachability.IsReachableAsync(device.IP, device.PORT, 5000, stoppingToken);
                if (!reachable)
                {
                    _logger.LogWarning("[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} at {IP}:{PORT} is unreachable. Skipping sync.",scheduleId, deviceId, device.IP, device.PORT);
                    return false;
                }

                _logger.LogInformation("[PQM.Console] Schedule {ScheduleId}: Starting sync for Device {DeviceId}.", scheduleId, deviceId);

                var result = await profileSyncService.SyncDeviceAllProfilesAsync(deviceId, stoppingToken);

                string finalStatus = result.Success ? "Online" : "Error";

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} completed. Status={Status}, Profiles={Succeeded}/{Attempted}",
                        scheduleId, deviceId, finalStatus, result.ProfilesSucceeded, result.ProfilesAttempted);
                }
                else
                {
                    _logger.LogWarning(
                        "[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} failed. Error={Error}",
                        scheduleId, deviceId, result.ErrorMessage);
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[PQM.Console] Schedule {ScheduleId}: Device {DeviceId} sync threw an exception.",
                    scheduleId,
                    deviceId);

                return false;
            }
        }
        private async Task<List<DueScheduleItem>> GetDueSchedulesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var nowUtc = DateTime.UtcNow;

            // Get all due global schedules.
            var list = await db.DeviceSyncSchedules
                .AsNoTracking()
                .Where(s => s.IsEnabled
                    && s.NextRunAtUtc != null
                    && s.NextRunAtUtc <= nowUtc)
                .OrderBy(s => s.Id)
                .Select(s => new DueScheduleItem
                {
                    ScheduleId = s.Id,
                    ScheduledTime = s.ScheduledTime,
                    RepeatMode = s.RepeatMode ?? "Daily",
                    TimeZoneId = "India Standard Time"
                })
                .ToListAsync(cancellationToken);

            // No due schedules.
            if (list.Count == 0)
                return list;

            // For each due schedule, get only the active devices assigned to THIS schedule
            // (Devices.ScheduleId is a FK -> DeviceSyncSchedule.Id: one device has exactly one schedule).
            foreach (var schedule in list)
            {
                var deviceIds = await db.Device
                    .AsNoTracking()
                    .Where(d => d.IsActive == true && d.IsDeleted == false
                             && d.DeviceSyncScheduleId == schedule.ScheduleId)
                    .OrderBy(d => d.Id)
                    .Select(d => d.Id)
                    .ToListAsync(cancellationToken);

                schedule.DeviceIds.AddRange(deviceIds);
            }
          
            return list;
        }
        private async Task UpdateScheduleCompletionAsync(int scheduleId, DateTime lastRunAtUtc, string lastRunStatus, DateTime? nextRunAtUtc, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            // ExecuteUpdateAsync issues a single UPDATE statement directly (EF Core 7+),
            // matching the original raw-SQL behavior without loading the entity first.
            await db.DeviceSyncSchedules
                .Where(s => s.Id == scheduleId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.LastRunAtUtc, lastRunAtUtc)
                        .SetProperty(s => s.LastRunStatus, lastRunStatus)
                        .SetProperty(s => s.NextRunAtUtc, nextRunAtUtc),
                    cancellationToken);
        }
    }
}