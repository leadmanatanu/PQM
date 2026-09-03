using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PQM.Core.DTOs;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/device")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly APIResponse _apiResponse;
        private readonly ILogger<DeviceController> _logger;
        private readonly string _connectionString;

        public DeviceController(
            IDeviceRepository deviceRepository,
            ILogger<DeviceController> logger,
            IConfiguration configuration)
        {
            _deviceRepository = deviceRepository;
            _apiResponse = new APIResponse();
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string DefaultConnection not found.");
        }

        private static string? FormatUtcIso(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            var utc = DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
            return utc.ToString("o");
        }

        [HttpGet]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var devices = await _deviceRepository.GetAllAsync(cancellationToken);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = devices;
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

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id, CancellationToken cancellationToken)
        {
            try
            {
                var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
                if (device == null) return NotFound();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = device;
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

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Device device, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _deviceRepository.AddAsync(device, cancellationToken);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.Created;
                _apiResponse.Data = created;
                _apiResponse.Errors.Clear();
                return CreatedAtAction(nameof(Get), new { id = created }, _apiResponse);
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

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromBody] Device device, CancellationToken cancellationToken)
        {
            try
            {
                if (id != device.Id) return BadRequest();

                var updated = await _deviceRepository.UpdateAsync(device, cancellationToken);
                if (!updated) return NotFound();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = updated;
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

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _deviceRepository.DeleteAsync(id, cancellationToken);
                if (!deleted) return NotFound();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = true;
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

        [HttpPost("{id:int}/sync")]
        public async Task<ActionResult> TriggerSync(int id, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(1) FROM Devices WHERE Id = @id AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));

                    if (count == 0)
                        return NotFound(new { error = $"Device {id} not found." });
                }

                using (var existingCmd = conn.CreateCommand())
                {
                    existingCmd.CommandText = "SELECT TOP 1 Id, Status FROM DeviceSyncRequest WHERE DeviceId = @id AND Status IN ('Pending', 'Processing') ORDER BY RequestedAt DESC";
                    existingCmd.Parameters.AddWithValue("@id", id);

                    using var reader = await existingCmd.ExecuteReaderAsync(cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        long existingId = reader.GetInt64(0);
                        string existingStatus = reader.GetString(1);

                        _apiResponse.Status = true;
                        _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                        _apiResponse.Data = new
                        {
                            requestId = existingId,
                            deviceId = id,
                            status = existingStatus,
                            message = $"Sync request already active (Request #{existingId}, Status: {existingStatus})."
                        };
                        _apiResponse.Errors.Clear();
                        return Ok(_apiResponse);
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO DeviceSyncRequest (DeviceId, RequestedAt, Status)
                        VALUES (@did, GETUTCDATE(), 'Pending');
                        SELECT SCOPE_IDENTITY();";

                    cmd.Parameters.AddWithValue("@did", id);
                    var requestId = await cmd.ExecuteScalarAsync(cancellationToken);

                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = new
                    {
                        requestId = Convert.ToInt64(requestId),
                        deviceId = id,
                        status = "Pending",
                        message = $"Sync request submitted for device {id}."
                    };
                    _apiResponse.Errors.Clear();
                    return Ok(_apiResponse);
                }
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

        public class DeviceScanRequest
        {
            public int? ProfileId { get; set; }
            public List<int>? ParameterIds { get; set; }
        }

        [HttpPost("{id:int}/scan")]
        public async Task<ActionResult> ScanDevice(int id, [FromBody] DeviceScanRequest? request, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(1) FROM Devices WHERE Id = @id AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));

                    if (count == 0)
                        return NotFound(new { error = $"Device {id} not found." });
                }

                using (var existingScanCmd = conn.CreateCommand())
                {
                    existingScanCmd.CommandText = "SELECT TOP 1 Id, Status FROM DeviceScanRequest WHERE DeviceId = @id AND Status IN ('Pending', 'Processing') ORDER BY RequestedAt DESC";
                    existingScanCmd.Parameters.AddWithValue("@id", id);

                    using var reader = await existingScanCmd.ExecuteReaderAsync(cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        long existingScanId = reader.GetInt64(0);
                        string existingStatus = reader.GetString(1);

                        _logger.LogInformation(
                            "[DeviceController] Live scan already active for Device {DeviceId} - ScanRequestId={ScanRequestId}, Status={Status}.",
                            id, existingScanId, existingStatus);

                        _apiResponse.Status = true;
                        _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                        _apiResponse.Data = new
                        {
                            scanRequestId = existingScanId,
                            deviceId = id,
                            status = existingStatus
                        };
                        _apiResponse.Errors.Clear();
                        return Ok(_apiResponse);
                    }
                }

                string? paramIdsJson = null;

                if (request?.ParameterIds != null && request.ParameterIds.Count > 0)
                    paramIdsJson = System.Text.Json.JsonSerializer.Serialize(request.ParameterIds);

                long scanRequestId;

                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.CommandText = @"
                        INSERT INTO DeviceScanRequest
                        (DeviceId, ProfileId, ParameterIds, Status, RequestedAt)
                        VALUES
                        (@deviceId, @profileId, @paramIds, 'Pending', GETUTCDATE());
                        SELECT SCOPE_IDENTITY();";

                    insertCmd.Parameters.AddWithValue("@deviceId", id);
                    insertCmd.Parameters.AddWithValue("@profileId", (object?)request?.ProfileId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@paramIds", (object?)paramIdsJson ?? DBNull.Value);

                    scanRequestId = Convert.ToInt64(
                        await insertCmd.ExecuteScalarAsync(cancellationToken));
                }

                _logger.LogInformation(
                    "[DeviceController] Live scan queued for Device {DeviceId} - ScanRequestId={ScanRequestId}.",
                    id, scanRequestId);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    scanRequestId,
                    deviceId = id,
                    status = "Pending"
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

        [HttpGet("{id:int}/scan/result/{scanRequestId:long}")]
        public async Task<ActionResult> GetScanResult(int id, long scanRequestId, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT Status, ResultJson, ErrorMessage
                    FROM DeviceScanRequest
                    WHERE Id = @id AND DeviceId = @deviceId";

                cmd.Parameters.AddWithValue("@id", scanRequestId);
                cmd.Parameters.AddWithValue("@deviceId", id);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                    return NotFound(new { error = $"Scan request {scanRequestId} not found for device {id}." });

                string status = reader.GetString(0);
                string? resultJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? errorMessage = reader.IsDBNull(2) ? null : reader.GetString(2);

                if (status == "Completed" && resultJson != null)
                {
                    var resultData = System.Text.Json.JsonSerializer.Deserialize<object>(resultJson);

                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = resultData;
                    _apiResponse.Errors.Clear();

                    return Ok(_apiResponse);
                }

                if (status == "Failed")
                {
                    bool isConcurrency =
                        errorMessage?.Contains("already syncing") == true ||
                        errorMessage?.Contains("already scanning") == true;

                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = isConcurrency
                        ? System.Net.HttpStatusCode.Conflict
                        : System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string>
                    {
                        errorMessage ?? "Scan failed."
                    };

                    return isConcurrency
                        ? StatusCode(409, _apiResponse)
                        : Ok(_apiResponse);
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    scanRequestId,
                    deviceId = id,
                    status
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

        [HttpPost("{id:int}/enable-sync")]
        public async Task<ActionResult> EnableSync(int id, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(1) FROM Devices WHERE Id = @id AND (IsDeleted = 0 OR IsDeleted IS NULL)";
                checkCmd.Parameters.AddWithValue("@id", id);

                var count = Convert.ToInt32(
                    await checkCmd.ExecuteScalarAsync(cancellationToken));

                if (count == 0)
                    return NotFound(new { error = $"Device {id} not found." });

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    deviceId = id,
                    isEnabled = true,
                    message = "Device sync enabled."
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
                    SELECT
                        Id,
                        IsEnabled,
                        ScheduledTime,
                        RepeatMode,
                        NextRunAtUtc,
                        LastRunAtUtc,
                        LastRunStatus
                    FROM DeviceSyncSchedule
                    ORDER BY ScheduledTime ASC;";

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    list.Add(new
                    {
                        id = reader.GetInt32(0),
                        isEnabled = reader.GetBoolean(1),
                        scheduledTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                        repeatMode = reader.GetString(3),
                        nextRunAtUtc = reader.IsDBNull(4)
                            ? null
                            : FormatUtcIso(reader.GetDateTime(4)),
                        lastRunAtUtc = reader.IsDBNull(5)
                            ? null
                            : FormatUtcIso(reader.GetDateTime(5)),
                        lastRunStatus = reader.IsDBNull(6)
                            ? null
                            : reader.GetString(6)
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

        [HttpGet("meterTypes")]
        public async Task<ActionResult> GetAllMeterTypes(CancellationToken cancellationToken)
        {
            try
            {
                var list = new List<object>();

                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
            SELECT
                Id,
                Name
            FROM MeterType
            ORDER BY Name ASC;";

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    list.Add(new
                    {
                        id = reader.GetInt32(0),
                        name = reader.GetString(1)
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

        [HttpGet("schedule/{id:int}")]
        public async Task<ActionResult> GetSchedule(int id, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT
                        Id,
                        IsEnabled,
                        ScheduledTime,
                        RepeatMode,
                        NextRunAtUtc,
                        LastRunAtUtc,
                        LastRunStatus
                    FROM DeviceSyncSchedule
                    WHERE Id = @id;";

                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                    return NotFound(new { error = $"Schedule {id} not found." });

                var data = new
                {
                    id = reader.GetInt32(0),
                    isEnabled = reader.GetBoolean(1),
                    scheduledTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                    repeatMode = reader.GetString(3),
                    nextRunAtUtc = reader.IsDBNull(4)
                        ? null
                        : FormatUtcIso(reader.GetDateTime(4)),
                    lastRunAtUtc = reader.IsDBNull(5)
                        ? null
                        : FormatUtcIso(reader.GetDateTime(5)),
                    lastRunStatus = reader.IsDBNull(6)
                        ? null
                        : reader.GetString(6)
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
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

        [HttpPost("schedule")]
        public async Task<ActionResult> CreateSchedule(
            [FromBody] UpdateScheduleRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!TimeSpan.TryParse(request.ScheduledTime, out var scheduledTime))
                {
                    return BadRequest(new
                    {
                        error = "Invalid ScheduledTime format. Expected HH:mm or HH:mm:ss."
                    });
                }

                DateTime nowUtc = DateTime.UtcNow;
                string timeZoneId = "India Standard Time";

                DateTime? nextRunAtUtc = request.IsEnabled
                    ? PQM.Core.Helpers.ScheduleHelper.ComputeNextRunAtUtc(
                        scheduledTime,
                        timeZoneId,
                        nowUtc)
                    : null;

                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    INSERT INTO DeviceSyncSchedule
                    (
                        IsEnabled,
                        ScheduledTime,
                        RepeatMode,
                        NextRunAtUtc
                    )
                    VALUES
                    (
                        @isEnabled,
                        @scheduledTime,
                        @repeatMode,
                        @nextRunAtUtc
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                cmd.Parameters.AddWithValue("@isEnabled", request.IsEnabled);
                cmd.Parameters.AddWithValue("@scheduledTime", scheduledTime);
                cmd.Parameters.AddWithValue("@repeatMode", request.RepeatMode ?? "Daily");
                cmd.Parameters.AddWithValue(
                    "@nextRunAtUtc",
                    (object?)nextRunAtUtc ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                int scheduleId = Convert.ToInt32(result);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    id = scheduleId,
                    isEnabled = request.IsEnabled,
                    scheduledTime = scheduledTime.ToString(@"hh\:mm"),
                    repeatMode = request.RepeatMode ?? "Daily",
                    nextRunAtUtc = FormatUtcIso(nextRunAtUtc)
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

        [HttpPut("schedule/{id:int}")]
        public async Task<ActionResult> UpdateSchedule(
            int id,
            [FromBody] UpdateScheduleRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!TimeSpan.TryParse(request.ScheduledTime, out var scheduledTime))
                {
                    return BadRequest(new
                    {
                        error = "Invalid ScheduledTime format. Expected HH:mm or HH:mm:ss."
                    });
                }

                DateTime nowUtc = DateTime.UtcNow;
                string timeZoneId = "India Standard Time";

                DateTime? nextRunAtUtc = request.IsEnabled
                    ? PQM.Core.Helpers.ScheduleHelper.ComputeNextRunAtUtc(
                        scheduledTime,
                        timeZoneId,
                        nowUtc)
                    : null;

                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    UPDATE DeviceSyncSchedule
                    SET
                        IsEnabled = @isEnabled,
                        ScheduledTime = @scheduledTime,
                        RepeatMode = @repeatMode,
                        NextRunAtUtc = @nextRunAtUtc
                    WHERE Id = @id;";

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@isEnabled", request.IsEnabled);
                cmd.Parameters.AddWithValue("@scheduledTime", scheduledTime);
                cmd.Parameters.AddWithValue("@repeatMode", request.RepeatMode ?? "Daily");
                cmd.Parameters.AddWithValue(
                    "@nextRunAtUtc",
                    (object?)nextRunAtUtc ?? DBNull.Value);

                int rowsAffected =
                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (rowsAffected == 0)
                    return NotFound(new { error = $"Schedule {id} not found." });

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    id,
                    isEnabled = request.IsEnabled,
                    scheduledTime = scheduledTime.ToString(@"hh\:mm"),
                    repeatMode = request.RepeatMode ?? "Daily",
                    nextRunAtUtc = FormatUtcIso(nextRunAtUtc)
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
    }
}