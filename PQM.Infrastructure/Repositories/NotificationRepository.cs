using Microsoft.EntityFrameworkCore;
using PQM.Core.DTOs.Notifications;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Core.Events;

namespace PQM.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly DataContext _db;
        public NotificationRepository(DataContext db,IEventPublisher eventPublisher)
        {
            _db = db;
        }
        public async Task<IEnumerable<NotificationDto>> GetAllNotificationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await (from r in _db.NotificationRecipients
                          join n in _db.Notifications on r.NotificationId equals n.Id
                          where r.UserId == userId
                          orderby n.CreatedAt descending
                          select new NotificationDto
                          {
                              Id = n.Id,
                              Title = n.Title,
                              Message = n.Message,
                              Type = n.Type,
                              Severity = n.Severity,
                              IsRead = r.IsRead,
                              CreatedAt = n.CreatedAt,
                              ReadAt = r.ReadAt
                          }).ToListAsync(cancellationToken);
        }
        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) => _db.NotificationRecipients.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
        public async Task<NotificationDto?> GetByIdAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        {
            return await (from r in _db.NotificationRecipients
                          join n in _db.Notifications on r.NotificationId equals n.Id
                          where r.NotificationId == notificationId && r.UserId == userId
                          select new NotificationDto
                          {
                              Id = n.Id,
                              Title = n.Title,
                              Message = n.Message,
                              Type = n.Type,
                              Severity = n.Severity,
                              IsRead = r.IsRead,
                              CreatedAt = n.CreatedAt,
                              ReadAt = r.ReadAt
                          }).FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<Notification> CreateAsync(Notification notification,CancellationToken cancellationToken = default)
        {
            notification.CreatedAt = DateTime.Now;

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            var userIds = notification.Type == "Schedule"
                ? await _db.User
                    .Where(x => x.RoleId == 1 || x.RoleId == 2)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken)

                : await _db.User
                    .Where(x => x.RoleId == 1)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

            foreach (var userId in userIds)
            {
                _db.NotificationRecipients.Add(new NotificationRecipient
                {
                    NotificationId = notification.Id,
                    UserId = userId
                });
            }


            await _db.SaveChangesAsync(cancellationToken);

            return notification;

        }
        public async Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        {
            var r = await _db.NotificationRecipients.FirstOrDefaultAsync(
                x => x.NotificationId == notificationId && x.UserId == userId, cancellationToken);

            if (r == null) return false;

            r.IsRead = true;
            r.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var list = await _db.NotificationRecipients
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync(cancellationToken);

            if (!list.Any()) return false;

            foreach (var r in list)
            {
                r.IsRead = true;
                r.ReadAt = DateTime.Now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}