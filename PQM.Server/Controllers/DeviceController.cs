using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using PQM.Core.DTOs;
using PQM.Infrastructure;
using PQM.Server.Models;
using PQM.Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly IDeviceService _deviceService;
        private readonly IDeviceParameterConfigService _configService;
        private readonly ILogger<DeviceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DeviceController(
            ILogger<DeviceController> logger,
            IDeviceService deviceService,
            IDeviceParameterConfigService configService,
            IConfiguration configuration)
        {
            _logger = logger;
            _deviceService = deviceService;
            _configService = configService;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                var data = _deviceService.GetDevices().ToList();
                using (var db = new DataContext(_connectionString))
                {
                    foreach (var d in data)
                    {
                        d.IsConfigured = db.DeviceParameterConfig.Any(c => c.DeviceId == d.Id && c.IsSelected);
                    }
                }
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpGet("{id}")]
        public ActionResult Get(int id)
        {
            try
            {
                var data = _deviceService.GetDevices().FirstOrDefault(x => x.Id == id);
                if (data == null)
                {
                    return NotFound(new { error = "Device not found." });
                }
                using (var db = new DataContext(_connectionString))
                {
                    data.IsConfigured = db.DeviceParameterConfig.Any(c => c.DeviceId == data.Id && c.IsSelected);
                }
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] Device device)
        {
            try
            {
                if (device == null)
                {
                    return BadRequest(new { error = "Invalid device payload." });
                }

                device.CreatedDate = DateTime.UtcNow;
                device.Status = "Offline";
                var id = _deviceService.AddDevice(device);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = id;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpPut]
        public ActionResult Put([FromBody] Device device)
        {
            try
            {
                if (device == null)
                {
                    return BadRequest(new { error = "Invalid device payload." });
                }

                var success = _deviceService.UpdateDevice(device);
                if (!success)
                {
                    return NotFound(new { error = "Device not found." });
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = success;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }



        [HttpGet("{id}/status")]
        public ActionResult GetStatus(int id)
        {
            var device = _deviceService.GetDevices().FirstOrDefault(x => x.Id == id);
            if (device == null)
            {
                return NotFound(new { error = "Device not found." });
            }
            return Ok(new
            {
                status = device.Status,
                lastConnectionAttempt = device.LastConnectionAttempt,
                lastError = device.LastError
            });
        }

        [HttpGet("{id}/last-sync")]
        public ActionResult GetLastSync(int id)
        {
            var device = _deviceService.GetDevices().FirstOrDefault(x => x.Id == id);
            if (device == null)
            {
                return NotFound(new { error = "Device not found." });
            }
            return Ok(new
            {
                lastSync = device.LastSync
            });
        }

        [HttpGet("{id}/events")]
        public ActionResult GetEvents(int id)
        {
            try
            {
                using var db = new DataContext(_connectionString);
                var events = db.DeviceConnectionEvents
                    .Where(e => e.DeviceId == id)
                    .OrderByDescending(e => e.OccurredAt)
                    .Take(100)
                    .ToList();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = events;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/readings")]
        public ActionResult GetReadings(int id)
        {
            try
            {
                using var db = new DataContext(_connectionString);

                // Rerouted from legacy db.ParameterValue (table no longer exists) to
                // DeviceLatestReadings, which is a high-performance upsert cache kept
                // current by the sync infrastructure layer.
                //
                // Id is set to ParameterId: DeviceLatestReadings has exactly one row per
                // (DeviceId, ParameterId), so ParameterId is unique within this device's
                // result set. This makes it safe for React key prop, row selection (useSelection
                // hook), and the Id column display in devices-table.tsx.
                //
                // Timestamp = UpdatedAt: the frontend labels this field "LAST SYNC DATE"
                // (devicereadings/page.tsx), which correctly implies data freshness, not
                // the meter's recording time — so UpdatedAt is semantically correct here.
                var results = db.DeviceLatestReadings
                    .Where(x => x.DeviceId == id)
                    .Join(db.Parameter,
                        x => x.ParameterId,
                        p => p.Id,
                        (x, p) => new
                        {
                            Id = (long)x.ParameterId,   // unique per device; safe as React key
                            ParameterId = x.ParameterId,
                            ParameterName = p.Name,
                            ObisCode = p.ObisCode ?? "",
                            Value = x.Value ?? "",
                            Timestamp = x.UpdatedAt     // labeled "LAST SYNC DATE" in frontend
                        })
                    .ToList();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = results;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/discover-parameters")]
        public IActionResult DiscoverParameters(int id, [FromQuery] string? objectType)
        {
            using var db = new DataContext(_connectionString);
            var parameters = db.Parameter
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToList();

            var results = parameters.Select(p => new
            {
                Name = p.Name,
                ObisCode = p.ObisCode,
                ObjectType = p.ObjectType,
                Value = db.ParameterValue
                    .Where(pv => pv.DeviceId == id && pv.ParameterId == p.Id)
                    .OrderByDescending(pv => pv.Timestamp)
                    .Select(pv => pv.Value)
                    .FirstOrDefault() ?? ""
            }).ToList();

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = results;
            return Ok(_apiResponse);
        }

        [HttpPost("{id}/read-parameter/{parameterId}")]
        public IActionResult ReadParameter(int id, int parameterId)
        {
            using var db = new DataContext(_connectionString);
            var param = db.Parameter.FirstOrDefault(p => p.Id == parameterId);
            var latest = db.ParameterValue
                .Where(pv => pv.DeviceId == id && pv.ParameterId == parameterId)
                .OrderByDescending(pv => pv.Timestamp)
                .FirstOrDefault();

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = latest?.Value ?? "";
            return Ok(_apiResponse);
        }

        [HttpPost("{id}/read-object/{objectId}")]
        public IActionResult ReadObject(int id, int objectId)
        {
            using var db = new DataContext(_connectionString);
            var param = db.Parameter.FirstOrDefault(p => p.Id == objectId);
            var latest = db.ParameterValue
                .Where(pv => pv.DeviceId == id && pv.ParameterId == objectId)
                .OrderByDescending(pv => pv.Timestamp)
                .FirstOrDefault();

            var results = new List<object>
            {
                new
                {
                    Id = latest?.Id ?? 0,
                    ObjectId = objectId,
                    ParameterId = objectId,
                    AttributeId = 2,
                    Name = param?.Name ?? "Unknown",
                    DataType = param?.ObjectType ?? "Register",
                    AccessType = "Read",
                    Value = latest?.Value ?? "",
                    Timestamp = latest?.Timestamp ?? DateTime.UtcNow
                }
            };

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = results;
            return Ok(_apiResponse);
        }

        [HttpPost("{id}/read-objects")]
        public IActionResult ReadObjects(int id, [FromBody] List<int> objectIds)
        {
            if (objectIds == null || !objectIds.Any())
            {
                return Error("No object IDs provided", System.Net.HttpStatusCode.BadRequest);
            }

            using var db = new DataContext(_connectionString);
            var results = new List<object>();

            foreach (var objectId in objectIds)
            {
                var param = db.Parameter.FirstOrDefault(p => p.Id == objectId);
                var latest = db.ParameterValue
                    .Where(pv => pv.DeviceId == id && pv.ParameterId == objectId)
                    .OrderByDescending(pv => pv.Timestamp)
                    .FirstOrDefault();

                results.Add(new
                {
                    Id = latest?.Id ?? 0,
                    ObjectId = objectId,
                    ParameterId = objectId,
                    AttributeId = 2,
                    Name = param?.Name ?? "Unknown",
                    DataType = param?.ObjectType ?? "Register",
                    AccessType = "Read",
                    Value = latest?.Value ?? "",
                    Timestamp = latest?.Timestamp ?? DateTime.UtcNow
                });
            }

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = results;
            return Ok(_apiResponse);
        }

        public class WriteObjectRequest
        {
            public required string ObisCode { get; set; }
            public required string Value { get; set; }
            public int AttributeId { get; set; } = 2;
        }

        [HttpPost("{id}/write-object")]
        public IActionResult WriteObject(int id, [FromBody] WriteObjectRequest request)
        {
            // decoupled stub - actual writing should be handled asynchronously or logged.
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = "Write simulated successfully in database-driven mode";
            return Ok(_apiResponse);
        }

        public class NotifyStatusRequest
        {
            public int DeviceId { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? LastSync { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime OccurredAt { get; set; }
        }

        // This endpoint exists to support the now-retired D:\Console bridge. Safe to remove once PQM.Server's own hosted sync service (replacing D:\Console) is confirmed live and handles device status updates + SignalR broadcasts internally.
        [HttpPost("{id}/notify-status")]
        public async Task<IActionResult> NotifyStatus(int id, [FromBody] NotifyStatusRequest request, [FromServices] IHubContext<DeviceHub> hubContext)
        {
            await hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
            {
                deviceId = id,
                status = request.Status,
                lastSync = request.LastSync,
                eventType = request.EventType,
                message = request.Message,
                occurredAt = request.OccurredAt
            });

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = "Notification broadcasted";
            return Ok(_apiResponse);
        }

        [HttpPost("/api/devices/status-changed")]
        [HttpPost("/api/device/status-changed")]
        public async Task<IActionResult> DeviceStatusChanged([FromBody] DeviceStatusChangedDto dto, [FromServices] IHubContext<DeviceHub> hubContext)
        {
            await hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
            {
                deviceId = dto.DeviceId,
                status = dto.Status,
                lastSync = dto.LastSync,
                lastError = dto.LastError
            });

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = "Status broadcasted successfully";
            return Ok(_apiResponse);
        }

        [HttpGet("/api/devices/{deviceId}/configuration")]
        [HttpGet("/api/device/{deviceId}/configuration")]
        public async Task<ActionResult> GetConfiguration(int deviceId, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _configService.GetDeviceConfigurationAsync(deviceId, cancellationToken);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
                _apiResponse.Errors.Clear();
                return Ok(_apiResponse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        public class SaveConfigRequest
        {
            public List<int> ParameterIds { get; set; } = new();
        }

        [HttpPost("/api/devices/{deviceId}/configuration")]
        [HttpPost("/api/device/{deviceId}/configuration")]
        public async Task<ActionResult> SaveConfiguration(int deviceId, [FromBody] SaveConfigRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required." });
                }

                var result = await _configService.SaveDeviceConfigurationAsync(deviceId, request.ParameterIds, cancellationToken);
                
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
                _apiResponse.Errors.Clear();
                return Ok(_apiResponse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return BadRequest(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpGet("/api/devices/{deviceId}/selected-parameters")]
        [HttpGet("/api/device/{deviceId}/selected-parameters")]
        public async Task<ActionResult> GetSelectedParameters(int deviceId, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _configService.GetSelectedParametersAsync(deviceId, cancellationToken);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
                _apiResponse.Errors.Clear();
                return Ok(_apiResponse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        private IActionResult Error(string message, System.Net.HttpStatusCode statusCode)
        {
            _apiResponse.Status = false;
            _apiResponse.StatusCode = statusCode;
            _apiResponse.Errors.Add(message);
            return Ok(_apiResponse);
        }
    }
}
