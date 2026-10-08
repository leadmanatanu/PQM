using PQM.Core.DTOs;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Services
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<Notification?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}