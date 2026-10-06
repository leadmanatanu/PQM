using PQM.Server.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllNotificationsAsync(int userId , CancellationToken cancellationToken);

        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

        Task<Notification?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

        Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken cancellationToken = default);

        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}