using Gurux.DLMS.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.DTOs;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure.Services;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gurux.DLMS.Enums;
using SysTask = System.Threading.Tasks.Task;

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

        public DeviceController(IDeviceRepository deviceRepository,ILogger<DeviceController> logger,IConfiguration configuration)
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



        // SyncNow endpoint to trigger a sync request for a device
        [HttpPost("{id:int}/sync")]
        public async Task<ActionResult> TriggerSync(int id,CancellationToken cancellationToken)
        {
            try
            {
                // Get device first
                var device = await _deviceRepository.GetByIdAsync(
                    id,
                    cancellationToken);

                if (device == null)
                {
                    return NotFound(new
                    {
                        error = $"Device {id} not found."
                    });
                }

                // Check whether device is reachable
                bool reachable = await IsDeviceReachableAsync(device.IP,device.PORT,5000,cancellationToken);

                if (!reachable)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;

                    _apiResponse.Data = null;

                    _apiResponse.Errors = new List<string>
                    {
                        $"Unable to connect to device at {device.IP}:{device.PORT}. Check network connectivity and Power."
                    };

                    return Ok(_apiResponse);
                }

                using var conn =
                    new Microsoft.Data.SqlClient.SqlConnection(
                        _connectionString);

                await conn.OpenAsync(cancellationToken);

                // Check if an active sync request already exists
                using (var existingCmd = conn.CreateCommand())
                {
                    existingCmd.CommandText = @"
                SELECT TOP 1 Id, Status
                FROM DeviceSyncRequests
                WHERE DeviceId = @id
                  AND Status IN ('Pending', 'Processing')
                ORDER BY RequestedAt DESC";

                    existingCmd.Parameters.AddWithValue("@id", id);

                    using var reader =
                        await existingCmd.ExecuteReaderAsync(cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        long existingId = reader.GetInt64(0);
                        string existingStatus = reader.GetString(1);

                        _apiResponse.Status = true;
                        _apiResponse.StatusCode =
                            System.Net.HttpStatusCode.OK;

                        _apiResponse.Data = new
                        {
                            requestId = existingId,
                            deviceId = id,
                            status = existingStatus,
                            message =
                                $"Sync request already active (Request #{existingId}, Status: {existingStatus})."
                        };

                        _apiResponse.Errors.Clear();

                        return Ok(_apiResponse);
                    }
                }

                // Create new sync request
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                INSERT INTO DeviceSyncRequests
                (
                    DeviceId,
                    RequestedAt,
                    Status
                )
                VALUES
                (
                    @did,
                    GETUTCDATE(),
                    'Pending'
                );

                SELECT SCOPE_IDENTITY();";

                    cmd.Parameters.AddWithValue("@did", id);

                    var requestId =
                        await cmd.ExecuteScalarAsync(cancellationToken);

                    _apiResponse.Status = true;
                    _apiResponse.StatusCode =
                        System.Net.HttpStatusCode.OK;

                    _apiResponse.Data = new
                    {
                        requestId = Convert.ToInt64(requestId),
                        deviceId = id,
                        status = "Pending",
                        message =
                            $"Sync request submitted for device {id}."
                    };

                    _apiResponse.Errors.Clear();

                    return Ok(_apiResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[DeviceController] TriggerSync failed for Device {DeviceId}.",
                    id);

                _apiResponse.Status = false;
                _apiResponse.StatusCode =
                    System.Net.HttpStatusCode.BadRequest;

                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string>
                {
                    ex.Message
                };

                return Ok(_apiResponse);
            }
        }

        //schedule endpoints to manage device sync schedules
        [HttpPost("schedule")]
        public async Task<ActionResult> CreateSchedule([FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
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
        public async Task<ActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
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



        // Live scan endpoint with concurrency control
       
        [HttpPost("{id:int}/live-scan")]
        public async Task<ActionResult> LiveScan(int id, [FromBody] LiveScanRequest? request, CancellationToken cancellationToken)
        {
            // Overall request timeout — live scan should be fast, not 5 minutes
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);
            var ct = linkedCts.Token;

            // Fetch device first — needed both for the lock-busy message and everything after.
            var device = await _deviceRepository.GetByIdAsync(id, ct);
            if (device == null)
                return NotFound(new { error = $"Device {id} not found." });

            var deviceLock = GetDeviceLock(id);

            // Try to get the lock immediately — if another scan is already running
            // on this device, tell the frontend right away instead of queueing.
            bool acquired = await deviceLock.WaitAsync(TimeSpan.Zero, cancellationToken);

            if (!acquired)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.Conflict;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string>
                {
                    "A previous scan on this device is still in progress. If this persists, the device may be unresponsive — try again shortly."
                };
                return StatusCode(409, _apiResponse);
            }

            try
            {
                bool reachable = await IsDeviceReachableAsync(device.IP, device.PORT, 5000, ct);
                if (!reachable)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _apiResponse.Data = null;
                    _apiResponse.Errors = new List<string>
                    {
                        $"Unable to connect to device at {device.IP}:{device.PORT}. Check network connectivity and Power ."
                    };
                    return Ok(_apiResponse);
                }

                var items = await ReadLiveValuesFromMeterAsync(device, request?.ProfileIds, request?.ParameterIds, ct);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new
                {
                    scannedAt = DateTime.UtcNow.ToString("o"),
                    deviceId = id,
                    deviceName = device.Name,
                    items
                };
                _apiResponse.Errors.Clear();

                return Ok(_apiResponse);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.RequestTimeout;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { "Meter did not respond within 120 seconds." };
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeviceController] Live scan failed for Device {DeviceId}.", id);

                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
            finally
            {
                deviceLock.Release();
            }
        }

        private async Task<List<LiveScanItemResult>> ReadLiveValuesFromMeterAsync(Device device,List<int>? profileIds,List<int>? parameterIds,CancellationToken ct)
        {
            var parameters = new List<(int Id, string Name, string ObisCode, string? ObjectType, int? AttributeIndex, int? Scaler, string? Unit)>();

            using (var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                await conn.OpenAsync(ct);
                using var cmd = conn.CreateCommand();

                if (parameterIds != null && parameterIds.Count > 0)
                {
                    var idParams = string.Join(",", parameterIds.Select((_, i) => $"@id{i}"));
                    cmd.CommandText = $@"
                SELECT Id, Name, ObisCode, ObjectType, AttributeIndex, Scaler, Unit
                FROM Parameters
                WHERE Id IN ({idParams})
                  AND ObisCode IS NOT NULL
                  AND IsVisible = 1;";

                    for (int i = 0; i < parameterIds.Count; i++)
                        cmd.Parameters.Add($"@id{i}", System.Data.SqlDbType.Int).Value = parameterIds[i];
                }
                else if (profileIds != null && profileIds.Count > 0)
                {
                    var profileParams = string.Join(",", profileIds.Select((_, i) => $"@profileId{i}"));
                    cmd.CommandText = $@"
                SELECT Id, Name, ObisCode, ObjectType, AttributeIndex, Scaler, Unit
                FROM Parameters
                WHERE ProfileId IN ({profileParams})
                  AND ObisCode IS NOT NULL
                  AND IsVisible = 1;";

                    for (int i = 0; i < profileIds.Count; i++)
                        cmd.Parameters.Add($"@profileId{i}", System.Data.SqlDbType.Int).Value = profileIds[i];
                }
                else
                {
                    cmd.CommandText = @"
                SELECT TOP 50 Id, Name, ObisCode, ObjectType, AttributeIndex, Scaler, Unit
                FROM Parameters
                WHERE ObisCode IS NOT NULL
                  AND IsVisible = 1
                  AND (MeterTypeId = @meterTypeId OR MeterTypeId IS NULL)
                ORDER BY Id;";
                    cmd.Parameters.Add("@meterTypeId", System.Data.SqlDbType.Int).Value =
                        (object?)device.MeterTypeId ?? DBNull.Value;
                }

                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    parameters.Add((
                        Id: reader.GetInt32(0),
                        Name: reader.GetString(1),
                        ObisCode: reader.GetString(2),
                        ObjectType: reader.IsDBNull(3) ? null : reader.GetString(3),
                        AttributeIndex: reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        Scaler: reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        Unit: reader.IsDBNull(6) ? null : reader.GetString(6)
                    ));
                }
            }

            if (parameters.Count == 0)
                return new List<LiveScanItemResult>();

            var results = new List<LiveScanItemResult>();

            await using var meterReader = new DlmsMeterReader(device);
            await meterReader.ConnectAsync(ct);

            foreach (var param in parameters)
            {
                var item = new LiveScanItemResult
                {
                    ParameterId = param.Id,
                    ParameterName = param.Name,
                    ObisCode = param.ObisCode,
                    Unit = param.Unit
                };

                try
                {
                    GXDLMSObject dlmsObj = param.ObjectType switch
                    {
                        "GXDLMSExtendedRegister" => new GXDLMSExtendedRegister(param.ObisCode),
                        "GXDLMSDemandRegister" => new GXDLMSDemandRegister(param.ObisCode),
                        _ => new GXDLMSRegister(param.ObisCode)
                    };

                    if (param.Scaler.HasValue && dlmsObj is GXDLMSRegister reg)
                        reg.Scaler = param.Scaler.Value;
                    else if (param.Scaler.HasValue && dlmsObj is GXDLMSExtendedRegister extReg)
                        extReg.Scaler = param.Scaler.Value;

                    int attributeIndex = param.AttributeIndex ?? 2;
                    var value = await meterReader.ReadObjectAsync(dlmsObj, attributeIndex, ct);
                    item.Value = value?.ToString() ?? string.Empty;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    item.Error = ex.Message;
                    item.Value = string.Empty;
                }

                results.Add(item);
            }

            return results;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _deviceLocks = new();

        private static SemaphoreSlim GetDeviceLock(int deviceId) => _deviceLocks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));


        // Helper method to check if a device is reachable
        private static async System.Threading.Tasks.Task<bool> IsDeviceReachableAsync(string ip, int port, int timeoutMs, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = SysTask.Delay(timeoutMs, cancellationToken);

                var completed = await SysTask.WhenAny(connectTask, timeoutTask);

                if (completed == timeoutTask || !client.Connected)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
