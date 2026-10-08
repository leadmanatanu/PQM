using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs.Notifications;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Services;
using PQM.Server.Models;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _repo;
        private readonly APIResponse _response = new();
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationRepository repo, ILogger<NotificationController> logger)
        {
            _repo = repo;
            _logger = logger;
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

                var result = await _repo.CreateAsync(notification, [], ct);

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
    }
}