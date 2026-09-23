using PQM.Core.Events;
using Microsoft.AspNetCore.SignalR;
using PQM.Server.Hubs;

namespace PQM.Server.Services
{
    public class DeviceSyncNotificationHandler : IEventHandler<DeviceSyncCompletedEvent>
    {
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<DeviceSyncNotificationHandler> _logger;

        public DeviceSyncNotificationHandler(
            IHubContext<DeviceHub> hubContext,
            ILogger<DeviceSyncNotificationHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(DeviceSyncCompletedEvent @event)
        {
            // Same pattern as DevicePingBackgroundService
            var connections = DeviceHub.GetSubscribedConnections(@event.DeviceId);

            if (connections.Count == 0)
            {
                _logger.LogInformation("No subscriptions for Device {DeviceId}", @event.DeviceId);
                return;
            }

            try
            {
                await _hubContext.Clients
                    .Clients(connections)
                    .SendAsync(
                        "DeviceLastSyncChanged",
                        new
                        {
                            deviceId = @event.DeviceId,
                            lastSyncAt = @event.LastSyncAt
                        });

                _logger.LogInformation(
                    "Notified {ConnectionCount} clients about sync completion for Device {DeviceId}",
                    connections.Count,
                    @event.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to notify device sync completion for Device {DeviceId}",
                    @event.DeviceId);
            }
        }
    }
}