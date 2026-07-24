using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using PQM.Core.DTOs;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;
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
        private readonly ILogger<DeviceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DeviceController(
            ILogger<DeviceController> logger,
            IDeviceService deviceService,
            IConfiguration configuration)
        {
            _logger = logger;
            _deviceService = deviceService;
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

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                var success = _deviceService.DeleteDevice(id);
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

        [HttpPost("{id}/sync/now")]
        [HttpPost("/api/devices/{id}/sync/now")]
        public async Task<IActionResult> SyncNow(
            int id,
            [FromServices] ProfileSyncService syncService,
            [FromServices] IHubContext<DeviceHub> hubContext)
        {
            if (syncService.IsDeviceSyncing(id))
            {
                return Conflict(new
                {
                    status = false,
                    message = $"Sync is already in progress for device {id}."
                });
            }

            // Trigger background async sync for this device
            _ = Task.Run(async () =>
            {
                string finalStatus = "Error";
                string? finalError = "Sync failed";
                try
                {
                    await hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
                    {
                        deviceId = id,
                        status = "Syncing",
                        lastSync = (string?)null,
                        lastError = (string?)null
                    });

                    var result = await syncService.SyncDeviceAllProfilesAsync(id);
                    finalStatus = result.Success ? "Online" : "Error";
                    finalError = result.ErrorMessage;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing out-of-band sync for device {DeviceId}.", id);
                    finalError = ex.Message;
                }
                finally
                {
                    await hubContext.Clients.All.SendAsync("DeviceStatusChanged", new
                    {
                        deviceId = id,
                        status = finalStatus,
                        lastSync = DateTime.UtcNow.ToString("o"),
                        lastError = finalError
                    });
                }
            });

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.Accepted;
            _apiResponse.Data = $"Sync initiated for device {id}. Real-time progress will stream over SignalR.";
            return Accepted(_apiResponse);
        }

        [HttpPost("{id}/sync/enable")]
        [HttpPost("/api/devices/{id}/sync/enable")]
        public async Task<IActionResult> EnableSync(int id)
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Devices SET IsActive = 1 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound(new { error = $"Device {id} not found." });

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = $"Device {id} sync enabled (IsActive = true).";
            return Ok(_apiResponse);
        }

        [HttpPost("{id}/sync/disable")]
        [HttpPost("/api/devices/{id}/sync/disable")]
        public async Task<IActionResult> DisableSync(int id)
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Devices SET IsActive = 0, Status = 'Offline' WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound(new { error = $"Device {id} not found." });

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = $"Device {id} sync disabled (IsActive = false).";
            return Ok(_apiResponse);
        }

        [HttpGet("{id}/sync-history")]
        [HttpGet("/api/device/{id}/sync-history")]
        public async Task<ActionResult> GetSyncHistory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var rows = new List<object>();
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP 50
                        Id, DeviceId, StartedAt, CompletedAt, Status,
                        ErrorMessage, ProfilesRead, RowsWritten
                    FROM DeviceSyncHistory
                    WHERE DeviceId = @id
                    ORDER BY StartedAt DESC";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(new
                    {
                        id          = reader.GetInt64(0),
                        deviceId    = reader.GetInt32(1),
                        startedAt   = reader.GetDateTime(2),
                        completedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        status      = reader.GetString(4),
                        errorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                        profilesRead = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                        rowsWritten  = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
                    });
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = rows;
                _apiResponse.Errors.Clear();
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeviceController] Failed to retrieve sync history for device {DeviceId}.", id);
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }
        [HttpGet("{id}/schedule")]
        public async Task<ActionResult> GetSchedule(int id, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT s.DeviceId, s.IsEnabled, s.ScheduledTime, s.RepeatMode, s.NextRunAtUtc, s.LastRunAtUtc, s.LastRunStatus, d.TimeZoneId
                    FROM Devices d
                    LEFT JOIN DeviceSyncSchedule s ON d.Id = s.DeviceId
                    WHERE d.Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    bool hasSchedule = !reader.IsDBNull(1);
                    var data = new
                    {
                        deviceId = reader.GetInt32(0),
                        isEnabled = hasSchedule ? reader.GetBoolean(1) : false,
                        scheduledTime = hasSchedule ? reader.GetTimeSpan(2).ToString(@"hh\:mm") : "00:00",
                        repeatMode = hasSchedule ? reader.GetString(3) : "Daily",
                        nextRunAtUtc = hasSchedule && !reader.IsDBNull(4) ? reader.GetDateTime(4).ToString("o") : (string?)null,
                        lastRunAtUtc = hasSchedule && !reader.IsDBNull(5) ? reader.GetDateTime(5).ToString("o") : (string?)null,
                        lastRunStatus = hasSchedule && !reader.IsDBNull(6) ? reader.GetString(6) : (string?)null,
                        timeZoneId = reader.IsDBNull(7) ? "India Standard Time" : reader.GetString(7)
                    };
                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = data;
                    _apiResponse.Errors.Clear();
                    return Ok(_apiResponse);
                }

                return NotFound(new { error = $"Device {id} not found." });
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

        [HttpGet("schedules")]
        public async Task<ActionResult> GetAllSchedules(CancellationToken cancellationToken)
        {
            try
            {
                var list = new List<object>();
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT d.Id, d.Name, d.IP, d.Status, d.LastSync, d.TimeZoneId,
                           s.IsEnabled, s.ScheduledTime, s.RepeatMode, s.NextRunAtUtc, s.LastRunAtUtc, s.LastRunStatus
                    FROM Devices d
                    LEFT JOIN DeviceSyncSchedule s ON d.Id = s.DeviceId
                    WHERE d.IsDeleted = 0 OR d.IsDeleted IS NULL";
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    bool hasSchedule = !reader.IsDBNull(6);
                    list.Add(new
                    {
                        deviceId = reader.GetInt32(0),
                        deviceName = reader.GetString(1),
                        ip = reader.GetString(2),
                        status = reader.IsDBNull(3) ? "Offline" : reader.GetString(3),
                        lastSync = reader.IsDBNull(4) ? (string?)null : reader.GetDateTime(4).ToString("o"),
                        timeZoneId = reader.IsDBNull(5) ? "India Standard Time" : reader.GetString(5),
                        isEnabled = hasSchedule ? reader.GetBoolean(6) : false,
                        scheduledTime = hasSchedule ? reader.GetTimeSpan(7).ToString(@"hh\:mm") : "00:00",
                        repeatMode = hasSchedule ? reader.GetString(8) : "Daily",
                        nextRunAtUtc = hasSchedule && !reader.IsDBNull(9) ? reader.GetDateTime(9).ToString("o") : (string?)null,
                        lastRunAtUtc = hasSchedule && !reader.IsDBNull(10) ? reader.GetDateTime(10).ToString("o") : (string?)null,
                        lastRunStatus = hasSchedule && !reader.IsDBNull(11) ? reader.GetString(11) : (string?)null
                    });
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = list;
                _apiResponse.Errors.Clear();
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

        public class UpdateScheduleRequest
        {
            public bool IsEnabled { get; set; }
            public string ScheduledTime { get; set; } = "00:00";
            public string RepeatMode { get; set; } = "Daily";
        }

        [HttpPut("{id}/schedule")]
        public async Task<ActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required." });
                }

                if (!TimeSpan.TryParse(request.ScheduledTime, out var ts))
                {
                    return BadRequest(new { error = "Invalid ScheduledTime format. Expected HH:mm or HH:mm:ss." });
                }

                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                // Get device time zone
                string timeZoneId = "India Standard Time";
                using (var getTzCmd = conn.CreateCommand())
                {
                    getTzCmd.CommandText = "SELECT TimeZoneId FROM Devices WHERE Id = @id";
                    getTzCmd.Parameters.AddWithValue("@id", id);
                    var tzObj = await getTzCmd.ExecuteScalarAsync(cancellationToken);
                    if (tzObj == null || tzObj == DBNull.Value)
                    {
                        return NotFound(new { error = $"Device {id} not found." });
                    }
                    timeZoneId = Convert.ToString(tzObj) ?? "India Standard Time";
                }

                DateTime nowUtc = DateTime.UtcNow;
                DateTime? nextRunAtUtc = request.IsEnabled
                    ? PQM.Server.Services.DeviceScheduleRunnerService.ComputeNextRunAtUtc(ts, timeZoneId, nowUtc)
                    : null;

                using (var upsertCmd = conn.CreateCommand())
                {
                    upsertCmd.CommandText = @"
                        MERGE DeviceSyncSchedule AS target
                        USING (SELECT @id AS DeviceId) AS source
                        ON (target.DeviceId = source.DeviceId)
                        WHEN MATCHED THEN
                            UPDATE SET IsEnabled = @isEnabled, ScheduledTime = @scheduledTime, RepeatMode = @repeatMode, NextRunAtUtc = @nextRunAtUtc
                        WHEN NOT MATCHED THEN
                            INSERT (DeviceId, IsEnabled, ScheduledTime, RepeatMode, NextRunAtUtc)
                            VALUES (@id, @isEnabled, @scheduledTime, @repeatMode, @nextRunAtUtc);";
                    upsertCmd.Parameters.AddWithValue("@id", id);
                    upsertCmd.Parameters.AddWithValue("@isEnabled", request.IsEnabled);
                    upsertCmd.Parameters.AddWithValue("@scheduledTime", ts);
                    upsertCmd.Parameters.AddWithValue("@repeatMode", request.RepeatMode ?? "Daily");
                    upsertCmd.Parameters.AddWithValue("@nextRunAtUtc", (object?)nextRunAtUtc ?? DBNull.Value);

                    await upsertCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    deviceId = id,
                    isEnabled = request.IsEnabled,
                    scheduledTime = ts.ToString(@"hh\:mm"),
                    repeatMode = request.RepeatMode ?? "Daily",
                    nextRunAtUtc = nextRunAtUtc?.ToString("o")
                };
                _apiResponse.Errors.Clear();
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

        private IActionResult Error(string message, System.Net.HttpStatusCode statusCode)
        {
            _apiResponse.Status = false;
            _apiResponse.StatusCode = statusCode;
            _apiResponse.Errors.Add(message);
            return Ok(_apiResponse);
        }
    }
}
