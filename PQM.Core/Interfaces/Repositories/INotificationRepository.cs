using PQM.Core.DTOs.Notifications;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Services
{
    public interface INotificationRepository
    {
        Task<IEnumerable<NotificationDto>> GetAllNotificationsAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<NotificationDto?> GetByIdAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<Notification> CreateAsync(Notification notification, IEnumerable<int> userIds, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}