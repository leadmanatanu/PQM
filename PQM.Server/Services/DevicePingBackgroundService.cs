using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using PQM.Server.Hubs;
using PQM.Infrastructure.Services;

namespace PQM.Server.Services
{
    public class DevicePingBackgroundService : BackgroundService
    {
        private readonly string _connectionString;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<DevicePingBackgroundService> _logger;
        private readonly INetworkReachabilityService _reachability;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15); //connnection check interval
        private const int PingTimeoutMs = 3000;

        public DevicePingBackgroundService(IConfiguration configuration,IHubContext<DeviceHub> hubContext,ILogger<DevicePingBackgroundService> logger,INetworkReachabilityService reachability)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _hubContext = hubContext;
            _logger = logger;
            _reachability = reachability;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DevicePingBackgroundService] Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAllDevicesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DevicePingBackgroundService] Error during ping cycle.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("[DevicePingBackgroundService] Stopped.");
        }

        private async Task CheckAllDevicesAsync(CancellationToken cancellationToken)
        {
            var devices = new List<(int Id, string IP, int Port)>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, IP, PORT
                    FROM Devices
                    WHERE IsDeleted = 0 OR IsDeleted IS NULL";

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    devices.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
                }
            }

            await Task.WhenAll(devices.Select(d => CheckAndNotifyAsync(d.Id, d.IP, d.Port, cancellationToken)));
        }

        private async Task CheckAndNotifyAsync(int deviceId, string ip, int port, CancellationToken cancellationToken)
        {
            bool isOnline = await _reachability.IsReachableAsync(ip, port, PingTimeoutMs, cancellationToken);

            var connections = DeviceHub.GetSubscribedConnections(deviceId);
            if (connections.Count == 0) return;

            try
            {
                await _hubContext.Clients.Clients(connections).SendAsync("DeviceConnectionStatusChanged", new { deviceId, isOnline }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DevicePingBackgroundService] Failed to notify status for Device {DeviceId}.", deviceId);
            }
        }
    }
}