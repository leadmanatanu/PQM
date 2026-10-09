using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs.Notifications;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Models;
using Microsoft.EntityFrameworkCore;
using PQM.Core.Events;
using PQM.Infrastructure;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _repo;
        private readonly APIResponse _response = new();
        private readonly ILogger<NotificationController> _logger;
        private readonly DataContext _db;
        private readonly IEventPublisher _eventPublisher;

        public NotificationController(INotificationRepository repo,ILogger<NotificationController> logger,DataContext db,IEventPublisher eventPublisher)
        {
            _repo = repo;
            _logger = logger;
            _db = db;
            _eventPublisher = eventPublisher;
        }

        [HttpGet("GetAll/{userId}")]
        public async Task<ActionResult> GetAll(int userId, CancellationToken ct)
        {
            try
            {
                _response.Status = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Data = await _repo.GetAllNotificationsAsync(userId, ct);
                _response.Errors.Clear();
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Error(ex);
            }
        }

        [HttpGet("GetUnreadCount/{userId}")]
        public async Task<ActionResult> GetUnreadCount(int userId, CancellationToken ct)
        {
            try
            {
                _response.Status = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Data = await _repo.GetUnreadCountAsync(userId, ct);
                _response.Errors.Clear();
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Error(ex);
            }
        }

        [HttpGet("GetById/{id}/{userId}")]
        public async Task<ActionResult> GetById(int id, int userId, CancellationToken ct)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id, userId, ct);

                if (data == null)
                {
                    _response.Status = false;
                    _response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    _response.Data = null;
                    _response.Errors = new() { "Notification not found." };
                    return Ok(_response);
                }

                _response.Status = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Data = data;
                _response.Errors.Clear();
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification");
                return Error(ex);
            }
        }

        [HttpPost("AddNotification")]
        public async Task<ActionResult> Add(CreateNotificationDto dto, CancellationToken ct)
        {
            try
            {
                var notification = new Notification
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    Severity = dto.Severity
                };

                var result = await _repo.CreateAsync(notification,ct);

                _response.Status = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Data = result;
                _response.Errors.Clear();
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification");
                return Error(ex);
            }
        }

        [HttpPut("MarkAsRead/{id}/{userId}")]
        public async Task<ActionResult> MarkAsRead(int id, int userId, CancellationToken ct)
        {
            try
            {
                var result = await _repo.MarkAsReadAsync(id, userId, ct);

                _response.Status = result;
                _response.StatusCode = result
                    ? System.Net.HttpStatusCode.OK
                    : System.Net.HttpStatusCode.NotFound;
                _response.Data = result;
                _response.Errors = result ? [] : new() { "Notification not found." };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Error(ex);
            }
        }

        [HttpPut("MarkAllAsRead/{userId}")]
        public async Task<ActionResult> MarkAllAsRead(int userId, CancellationToken ct)
        {
            try
            {
                var result = await _repo.MarkAllAsReadAsync(userId, ct);

                _response.Status = true;
                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.Data = result;
                _response.Errors.Clear();
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notifications as read");
                return Error(ex);
            }
        }

        private ActionResult Error(Exception ex)
        {
            _response.Status = false;
            _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            _response.Data = null;
            _response.Errors = new() { ex.Message };
            return Ok(_response);
        }

        [HttpPost("Dispatch/{id}")]
        public async Task<ActionResult> Dispatch(int id, CancellationToken ct)
        {
            try
            {
                var notification = await _db.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct);

                if (notification == null)
                    return NotFound(new { message = "Notification not found." });

                var recipientUserIds = await _db.NotificationRecipients
                    .AsNoTracking()
                    .Where(x => x.NotificationId == id)
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToListAsync(ct);

                await _eventPublisher.PublishAsync(new NotificationCreatedEvent
                {
                    NotificationId = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    Severity = notification.Severity,
                    CreatedAt = notification.CreatedAt,
                    RecipientUserIds = recipientUserIds
                });

                return Ok(new
                {
                    success = true,
                    notificationId = id,
                    recipientCount = recipientUserIds.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching notification {NotificationId}", id);
                return StatusCode(500, new { message = "Failed to dispatch notification." });
            }
        }
    }
}