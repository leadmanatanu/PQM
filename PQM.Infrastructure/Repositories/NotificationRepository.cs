using Microsoft.EntityFrameworkCore;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Entities;

namespace PQM.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly DataContext _db;
        
        public NotificationRepository(DataContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<IEnumerable<Notification>> GetAllNotificationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        }
        public async Task<Notification?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
        }
        public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
            notification.CreatedAt = DateTime.Now;
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);
            return notification;
        }
        public async Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
            if (notification == null)
                return false;
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);
            if (!notifications.Any())
                return false;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

    }
}