using PQM.Core.DTOs.Notifications;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<NotificationDto>> GetAllNotificationsAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<NotificationDto?> GetByIdAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<Notification> CreateAsync(Notification notification,CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
        Task<(Notification? Notification, List<int> RecipientUserIds)>GetNotificationForDispatchAsync(int notificationId,CancellationToken cancellationToken = default);
    }
}