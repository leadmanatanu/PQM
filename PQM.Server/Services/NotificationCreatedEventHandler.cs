using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PQM.Core.Events;
using PQM.Server.Hubs;

namespace PQM.Server.Services
{
    public class NotificationCreatedEventHandler : IEventHandler<NotificationCreatedEvent>
    {
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<NotificationCreatedEventHandler> _logger;
        public NotificationCreatedEventHandler(IHubContext<DeviceHub> hubContext,ILogger<NotificationCreatedEventHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }
        public async Task HandleAsync(NotificationCreatedEvent @event)
        {
            // LOG 1: Confirm that the event handler was triggered.
            _logger.LogInformation(
                "[Notification Debug] Event received. NotificationId={NotificationId}, Title={Title}, Type={Type}, Severity={Severity}, CreatedAt={CreatedAt}, Recipients={Recipients}",
                @event.NotificationId,
                @event.Title,
                @event.Type,
                @event.Severity,
                @event.CreatedAt,
                string.Join(",", @event.RecipientUserIds));

            _logger.LogInformation(
                "[Notification] Event handler triggered. NotificationId={NotificationId}, Type={Type}, RecipientUserIds={RecipientUserIds}",
                @event.NotificationId,
                @event.Type,
                string.Join(",", @event.RecipientUserIds));

            var notification = new
            {
                id = @event.NotificationId,
                title = @event.Title,
                message = @event.Message,
                type = @event.Type,
                severity = @event.Severity,
                createdAt = @event.CreatedAt
            };

            foreach (var userId in @event.RecipientUserIds.Distinct())
            {
                _logger.LogInformation(
                    "[Notification] Checking SignalR connections for UserId={UserId}",
                    userId);

                var connections = DeviceHub.GetUserConnections(userId);

                // LOG 2: Check whether the recipient has an active registered connection.
                _logger.LogInformation(
                    "[Notification Debug] UserId={UserId}, ConnectionCount={Count}, Connections={Connections}",
                    userId,
                    connections.Count,
                    string.Join(",", connections));

                _logger.LogInformation(
                    "[Notification] Found {ConnectionCount} connection(s) for UserId={UserId}. ConnectionIds={ConnectionIds}",
                    connections.Count,
                    userId,
                    string.Join(",", connections));

                if (connections.Count == 0)
                {
                    _logger.LogWarning(
                        "[Notification Debug] Delivery skipped. UserId={UserId} has no registered connection.",
                        userId);

                    _logger.LogWarning(
                        "[Notification] NOT SENT. No active registered SignalR connection for UserId={UserId}, NotificationId={NotificationId}",
                        userId,
                        @event.NotificationId);

                    continue;
                }

                try
                {
                    // LOG 3: Confirm that sending is about to start.
                    _logger.LogInformation(
                        "[Notification Debug] Sending notification {NotificationId} to UserId={UserId}, Connections={ConnectionIds}",
                        @event.NotificationId,
                        userId,
                        string.Join(",", connections));

                    _logger.LogInformation(
                        "[Notification] Sending ReceiveNotification. NotificationId={NotificationId}, UserId={UserId}",
                        @event.NotificationId,
                        userId);

                    await _hubContext.Clients
                        .Clients(connections)
                        .SendAsync("ReceiveNotification", notification);

                    // LOG 4: Confirm that SignalR SendAsync completed.
                    _logger.LogInformation(
                        "[Notification Debug] SendAsync completed for UserId={UserId}, NotificationId={NotificationId}",
                        userId,
                        @event.NotificationId);

                    _logger.LogInformation(
                        "[Notification] SignalR SendAsync completed. NotificationId={NotificationId}, UserId={UserId}, ConnectionCount={ConnectionCount}",
                        @event.NotificationId,
                        userId,
                        connections.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[Notification] FAILED to send. NotificationId={NotificationId}, UserId={UserId}",
                        @event.NotificationId,
                        userId);
                }
            }

            _logger.LogInformation(
                "[Notification] Event handler finished. NotificationId={NotificationId}",
                @event.NotificationId);
        }
    }
}