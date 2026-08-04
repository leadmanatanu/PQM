using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure;
using PQM.Server.Models;
using PQM.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                if (device == null)
                {
                    return NotFound();
                }
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
                if (id != device.Id)
                {
                    return BadRequest();
                }

                var updated = await _deviceRepository.UpdateAsync(device, cancellationToken);
                if (!updated)
                {
                    return NotFound();
                }
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
                if (!deleted)
                {
                    return NotFound();
                }
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
                    {
                        return NotFound(new { error = $"Device {id} not found." });
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

        /// <summary>
        /// Submits a live scan request. Returns a scanRequestId immediately.
        /// PQM.Console picks up the request, executes it against the meter, and stores results.
        /// Poll GET /api/device/{id}/scan/result/{scanRequestId} for completion.
        ///
        /// ARCHITECTURAL NOTE: This endpoint intentionally does NOT open a DlmsMeterReader
        /// connection directly. All DLMS/meter communication is owned exclusively by PQM.Console
        /// so that PQM.Server restarts cannot interrupt in-progress connections, and so that
        /// scan and scheduled-sync operations are serialized through a single process.
        /// Do NOT add direct DlmsMeterReader calls back to this controller.
        /// </summary>
        [HttpPost("{id:int}/scan")]
        public async Task<ActionResult> ScanDevice(int id, [FromBody] DeviceScanRequest? request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify device exists
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

                // Serialize ParameterIds as JSON for storage
                string? paramIdsJson = null;
                if (request?.ParameterIds != null && request.ParameterIds.Count > 0)
                    paramIdsJson = System.Text.Json.JsonSerializer.Serialize(request.ParameterIds);

                long scanRequestId;
                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.CommandText = @"
                        INSERT INTO DeviceScanRequest (DeviceId, ProfileId, ParameterIds, Status, RequestedAt)
                        VALUES (@deviceId, @profileId, @paramIds, 'Pending', GETUTCDATE());
                        SELECT SCOPE_IDENTITY();";
                    insertCmd.Parameters.AddWithValue("@deviceId", id);
                    insertCmd.Parameters.AddWithValue("@profileId", (object?)(request?.ProfileId) ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@paramIds", (object?)paramIdsJson ?? DBNull.Value);
                    scanRequestId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync(cancellationToken));
                }

                _logger.LogInformation("[DeviceController] Live scan queued for Device {DeviceId} — ScanRequestId={ScanRequestId}.", id, scanRequestId);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new { scanRequestId, deviceId = id, status = "Pending" };
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

        /// <summary>
        /// Polls the result of a previously submitted scan request.
        /// Returns status: Pending | Processing | Completed | Failed.
        /// When Completed, the data field contains scannedAt + items[].
        /// </summary>
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
                    bool isConcurrency = errorMessage?.Contains("already syncing") == true ||
                                        errorMessage?.Contains("already scanning") == true;
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = isConcurrency
                        ? System.Net.HttpStatusCode.Conflict
                        : System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string> { errorMessage ?? "Scan failed." };
                    return isConcurrency ? StatusCode(409, _apiResponse) : Ok(_apiResponse);
                }

                // Still Pending or Processing
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new { scanRequestId, deviceId = id, status };
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
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        MERGE DeviceSyncSchedule AS target
                        USING (SELECT @id AS DeviceId) AS source
                        ON (target.DeviceId = source.DeviceId)
                        WHEN MATCHED THEN
                            UPDATE SET IsEnabled = 1
                        WHEN NOT MATCHED THEN
                            INSERT (DeviceId, IsEnabled, ScheduledTime, RepeatMode)
                            VALUES (@id, 1, '00:00', 'Daily');";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new { deviceId = id, isEnabled = true, message = "Device sync enabled." };
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
                        lastSync = reader.IsDBNull(4) ? (string?)null : FormatUtcIso(reader.GetDateTime(4)),
                        timeZoneId = reader.IsDBNull(5) ? "India Standard Time" : reader.GetString(5),
                        isEnabled = hasSchedule ? reader.GetBoolean(6) : false,
                        scheduledTime = hasSchedule ? reader.GetTimeSpan(7).ToString(@"hh\:mm") : "00:00",
                        repeatMode = hasSchedule ? reader.GetString(8) : "Daily",
                        nextRunAtUtc = hasSchedule && !reader.IsDBNull(9) ? FormatUtcIso(reader.GetDateTime(9)) : (string?)null,
                        lastRunAtUtc = hasSchedule && !reader.IsDBNull(10) ? FormatUtcIso(reader.GetDateTime(10)) : (string?)null,
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

        [HttpGet("{id:int}/schedule")]
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
                        nextRunAtUtc = hasSchedule && !reader.IsDBNull(4) ? FormatUtcIso(reader.GetDateTime(4)) : (string?)null,
                        lastRunAtUtc = hasSchedule && !reader.IsDBNull(5) ? FormatUtcIso(reader.GetDateTime(5)) : (string?)null,
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

        public class UpdateScheduleRequest
        {
            public bool IsEnabled { get; set; }
            public string ScheduledTime { get; set; } = "00:00";
            public string RepeatMode { get; set; } = "Daily";
        }

        [HttpPut("{id:int}/schedule")]
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
                    ? PQM.Core.Helpers.ScheduleHelper.ComputeNextRunAtUtc(ts, timeZoneId, nowUtc)
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
