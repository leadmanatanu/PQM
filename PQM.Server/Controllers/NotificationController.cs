using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs.Notifications;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Entities;
using PQM.Server.Models;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationRepository notificationRepository,
            ILogger<NotificationController> logger)
        {
            _apiResponse = new APIResponse();
            _notificationRepository = notificationRepository
                ?? throw new ArgumentNullException(nameof(notificationRepository));
            _logger = logger;
        }

        [HttpGet("GetAll/{userId}")]
        public async Task<ActionResult> GetAllNotifications(
            int userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var notifications = await _notificationRepository
                    .GetAllNotificationsAsync(userId, cancellationToken);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = notifications;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting notifications for UserId {UserId}", userId);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }

        [HttpGet("GetUnreadCount/{userId}")]
        public async Task<ActionResult> GetUnreadCount(
            int userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var count = await _notificationRepository
                    .GetUnreadCountAsync(userId, cancellationToken);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = count;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting unread notification count for UserId {UserId}", userId);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }

        [HttpGet("GetById/{id}/{userId}")]
        public async Task<ActionResult> GetById(
            int id,
            int userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var notification = await _notificationRepository
                    .GetByIdAsync(id, userId, cancellationToken);

                if (notification == null)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string>
                    {
                        "Notification not found."
                    };

                    return Ok(_apiResponse);
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = notification;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting NotificationId {NotificationId}", id);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }

        [HttpPost("AddNotification")]
        public async Task<ActionResult> Add(
            [FromBody] CreateNotificationDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = dto.UserId,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                var result = await _notificationRepository
                    .AddAsync(notification, cancellationToken);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding notification for UserId {UserId}", dto.UserId);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }

        [HttpPut("MarkAsRead/{id}/{userId}")]
        public async Task<ActionResult> MarkAsRead(
            int id,
            int userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _notificationRepository
                    .MarkAsReadAsync(id, userId, cancellationToken);

                if (!result)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string>
                    {
                        "Notification not found."
                    };

                    return Ok(_apiResponse);
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while marking NotificationId {NotificationId} as read",
                    id);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }

        [HttpPut("MarkAllAsRead/{userId}")]
        public async Task<ActionResult> MarkAllAsRead(
            int userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _notificationRepository
                    .MarkAllAsReadAsync(userId, cancellationToken);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while marking all notifications as read for UserId {UserId}",
                    userId);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }
    }
}