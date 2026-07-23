using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;

namespace PQM.Infrastructure.Services
{
    public class SyncResult
    {
        public bool Success { get; set; }
        public int RowsRead { get; set; }
        public int RowsWritten { get; set; }
        public int RowsSkipped { get; set; }
        public DateTime? NewWatermarkUtc { get; set; }
        public string? ErrorMessage { get; set; }

        public override string ToString() =>
            $"SyncResult [Success={Success}, RowsRead={RowsRead}, RowsWritten={RowsWritten}, RowsSkipped={RowsSkipped}, NewWatermarkUtc={NewWatermarkUtc:yyyy-MM-dd HH:mm:ss UTC}, Error={ErrorMessage ?? "None"}]";
    }

    public class ProfileSyncService
    {
        private readonly string _connectionString;
        private readonly ILogger<ProfileSyncService> _logger;

        public ProfileSyncService(string connectionString, ILogger<ProfileSyncService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SyncResult> SyncDeviceProfileAsync(int deviceId, string obisCode)
        {
            var result = new SyncResult();

            try
            {
                // 1. Load Device entity
                Device? device = await LoadDeviceAsync(deviceId);
                if (device == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Device with Id={deviceId} not found.";
                    _logger.LogError("[ProfileSyncService] {ErrorMessage}", result.ErrorMessage);
                    return result;
                }

                // Determine Device TimeZone
                TimeZoneInfo deviceTz = GetDeviceTimeZone(device.TimeZoneId);

                // 2. Classify OBIS Code
                bool isTimeSeries = ProfileCatalog.TimeSeriesProfiles.ContainsKey(obisCode);
                bool isStaticOrMetadata = ProfileCatalog.StaticOrMetadataProfiles.ContainsKey(obisCode);

                if (!isTimeSeries && !isStaticOrMetadata)
                {
                    _logger.LogWarning("[ProfileSyncService] OBIS code {ObisCode} not found in ProfileCatalog; treating as TimeSeries by default.", obisCode);
                    isTimeSeries = true;
                }

                // Ensure Profile record exists in DB
                int profileId = await EnsureProfileAsync(obisCode, isTimeSeries);

                // 3 & 4. Determine Watermark / StartTime
                DateTime? startTimeLocal = null;
                DateTime? currentWatermarkUtc = null;

                if (isTimeSeries)
                {
                    currentWatermarkUtc = await GetLastReadWatermarkUtcAsync(deviceId, profileId);
                    if (currentWatermarkUtc.HasValue)
                    {
                        // Take LastReadTimestampUtc, subtract 1-hour safety overlap
                        DateTime watermarkWithSafetyUtc = currentWatermarkUtc.Value.AddHours(-1);

                        // Convert UTC watermark to device's local timezone
                        startTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(watermarkWithSafetyUtc, deviceTz);

                        _logger.LogInformation(
                            "[ProfileSyncService] TimeSeries sync for device {DeviceId} ('{DeviceName}'), profile '{ObisCode}': " +
                            "Existing Watermark UTC = {WatermarkUtc:yyyy-MM-dd HH:mm:ss UTC} (with 1h safety = {WatermarkSafetyUtc:yyyy-MM-dd HH:mm:ss UTC}). " +
                            "Converted Local StartTime = {StartTimeLocal:yyyy-MM-dd HH:mm:ss} (Tz: {TzId})",
                            deviceId, device.Name, obisCode, currentWatermarkUtc.Value, watermarkWithSafetyUtc, startTimeLocal.Value, deviceTz.Id);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[ProfileSyncService] TimeSeries sync for device {DeviceId} ('{DeviceName}'), profile '{ObisCode}': " +
                            "No existing watermark state found. Performing full buffer read (startTime = null).",
                            deviceId, device.Name, obisCode);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "[ProfileSyncService] Static/Metadata sync for device {DeviceId} ('{DeviceName}'), profile '{ObisCode}': " +
                        "Performing full buffer read (watermarks not used).",
                        deviceId, device.Name, obisCode);
                }

                // 5. Connect to meter and read profile rows
                IReadOnlyList<ProfileRow> rows;
                IReadOnlyList<ProfileColumnInfo> columns;

                await using (var reader = new DlmsMeterReader(device, verboseLogging: false))
                {
                    await reader.ConnectAsync();
                    await reader.ReadAssociationViewAsync();

                    // Read capture objects to get column descriptors
                    var profileObj = reader.GetProfileObjects().FirstOrDefault(p => p.LogicalName == obisCode);
                    if (profileObj != null)
                    {
                        columns = await reader.ReadCaptureObjectsAsync(profileObj);
                    }
                    else
                    {
                        columns = new List<ProfileColumnInfo>();
                    }

                    rows = await reader.ReadProfileAllEntriesAsync(obisCode, startTimeLocal);
                }

                result.RowsRead = rows.Count;
                _logger.LogInformation("[ProfileSyncService] Read {RowsCount} rows from meter for device {DeviceId}, profile '{ObisCode}'.", rows.Count, deviceId, obisCode);

                if (rows.Count == 0)
                {
                    result.Success = true;
                    return result;
                }

                // Ensure Parameter records exist for captured columns
                var parameterMap = await EnsureParametersAsync(profileId, columns);

                // 6, 7 & 8. Write rows in a single transaction
                DateTime syncExecutionTimeUtc = DateTime.UtcNow;
                DateTime? maxWrittenEntryUtc = null;

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using var tx = conn.BeginTransaction();

                    try
                    {
                        // Fetch existing EntryTimestampUtc for deduplication
                        var existingTimestamps = await GetExistingEntryTimestampsUtcAsync(conn, tx, deviceId, profileId);

                        for (int rIdx = 0; rIdx < rows.Count; rIdx++)
                        {
                            var row = rows[rIdx];

                            // 6. Convert local row timestamp to UTC
                            DateTime? entryTimestampUtc = null;
                            if (row.Timestamp.HasValue && row.Timestamp.Value.Year > 1)
                            {
                                try
                                {
                                    var localDt = DateTime.SpecifyKind(row.Timestamp.Value, DateTimeKind.Unspecified);
                                    entryTimestampUtc = TimeZoneInfo.ConvertTimeToUtc(localDt, deviceTz);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "[ProfileSyncService] Row {RIdx}: failed to convert local timestamp {LocalTs} to UTC.", rIdx, row.Timestamp);
                                    entryTimestampUtc = null;
                                }
                            }

                            // 7. Duplicate Check
                            if (entryTimestampUtc.HasValue && existingTimestamps.Contains(entryTimestampUtc.Value))
                            {
                                result.RowsSkipped++;
                                continue;
                            }

                            // Insert ReadingSessions row
                            long sessionId = await InsertReadingSessionAsync(conn, tx, deviceId, profileId, syncExecutionTimeUtc, entryTimestampUtc);

                            // Insert ReadingValues & Upsert DeviceLatestReadings
                            for (int cIdx = 0; cIdx < row.Values.Count; cIdx++)
                            {
                                int parameterId = parameterMap.ContainsKey(cIdx) ? parameterMap[cIdx] : 0;
                                if (parameterId == 0) continue;

                                var cellObj = row.Values[cIdx];
                                string formattedVal = ValueFormatter.FormatValue(cellObj);
                                string? rawVal = cellObj?.ToString();
                                double? numericVal = TryParseDouble(formattedVal);

                                await InsertReadingValueAsync(conn, tx, sessionId, parameterId, formattedVal, rawVal, numericVal);

                                // Step 2: Upsert DeviceLatestReadings
                                await UpsertDeviceLatestReadingAsync(conn, tx, deviceId, parameterId, formattedVal, rawVal, syncExecutionTimeUtc);
                            }

                            result.RowsWritten++;
                            if (entryTimestampUtc.HasValue)
                            {
                                existingTimestamps.Add(entryTimestampUtc.Value);
                                if (!maxWrittenEntryUtc.HasValue || entryTimestampUtc.Value > maxWrittenEntryUtc.Value)
                                {
                                    maxWrittenEntryUtc = entryTimestampUtc.Value;
                                }
                            }
                        }

                        // 8. Update DeviceProfileSyncState for TimeSeries profiles ONLY
                        if (isTimeSeries)
                        {
                            DateTime? watermarkToSave = maxWrittenEntryUtc ?? currentWatermarkUtc;
                            if (watermarkToSave.HasValue)
                            {
                                await UpsertDeviceProfileSyncStateAsync(conn, tx, deviceId, profileId, watermarkToSave.Value, syncExecutionTimeUtc);
                                result.NewWatermarkUtc = watermarkToSave;
                            }
                        }

                        await tx.CommitAsync();
                        result.Success = true;

                        _logger.LogInformation(
                            "[ProfileSyncService] Sync completed successfully for device {DeviceId}, profile '{ObisCode}'. " +
                            "RowsRead={RowsRead}, RowsWritten={RowsWritten}, RowsSkipped={RowsSkipped}, NewWatermarkUtc={WatermarkUtc:yyyy-MM-dd HH:mm:ss UTC}",
                            deviceId, obisCode, result.RowsRead, result.RowsWritten, result.RowsSkipped, result.NewWatermarkUtc);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "[ProfileSyncService] Transaction failed for device {DeviceId}, profile '{ObisCode}'. Rollback executed.", deviceId, obisCode);
                        result.Success = false;
                        result.ErrorMessage = $"Database transaction error: {ex.Message}";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProfileSyncService] Sync failed for device {DeviceId}, profile '{ObisCode}'.", deviceId, obisCode);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================

        private TimeZoneInfo GetDeviceTimeZone(string? timeZoneId)
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ProfileSyncService] Invalid TimeZoneId '{TimeZoneId}' on Device. Falling back to 'India Standard Time'.", timeZoneId);
                }
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }

        private async Task<Device?> LoadDeviceAsync(int deviceId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, IP, PORT, ClientAddress, ServerAddress,
                                       AuthenticationTypeId, Password, Timeout, TimeZoneId
                                FROM Devices WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", deviceId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Device
                {
                    Id                   = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name                 = reader.GetString(reader.GetOrdinal("Name")),
                    IP                   = reader.GetString(reader.GetOrdinal("IP")),
                    PORT                 = reader.GetInt32(reader.GetOrdinal("PORT")),
                    ClientAddress        = reader.IsDBNull(reader.GetOrdinal("ClientAddress"))        ? 16    : reader.GetInt32(reader.GetOrdinal("ClientAddress")),
                    ServerAddress        = reader.IsDBNull(reader.GetOrdinal("ServerAddress"))        ? 1     : reader.GetInt32(reader.GetOrdinal("ServerAddress")),
                    AuthenticationTypeId = reader.IsDBNull(reader.GetOrdinal("AuthenticationTypeId")) ? null  : reader.GetInt32(reader.GetOrdinal("AuthenticationTypeId")),
                    Password             = reader.IsDBNull(reader.GetOrdinal("Password"))             ? null  : reader.GetString(reader.GetOrdinal("Password")),
                    Timeout              = reader.IsDBNull(reader.GetOrdinal("Timeout"))              ? 30000 : reader.GetInt32(reader.GetOrdinal("Timeout")),
                    TimeZoneId           = reader.IsDBNull(reader.GetOrdinal("TimeZoneId"))           ? null  : reader.GetString(reader.GetOrdinal("TimeZoneId"))
                };
            }

            return null;
        }

        private async Task<int> EnsureProfileAsync(string obisCode, bool isTimeSeries)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ProfileId FROM Profiles WHERE ObisCode = @obis";
                cmd.Parameters.AddWithValue("@obis", obisCode);
                var existingId = await cmd.ExecuteScalarAsync();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            // Insert missing profile
            using (var cmd = conn.CreateCommand())
            {
                string friendlyName = ProfileCatalog.AllProfiles.GetValueOrDefault(obisCode, obisCode);
                string category = isTimeSeries ? "TimeSeries" : "Static";

                cmd.CommandText = @"INSERT INTO Profiles (ObisCode, FriendlyName, Category)
                                    VALUES (@obis, @name, @cat);
                                    SELECT SCOPE_IDENTITY();";
                cmd.Parameters.AddWithValue("@obis", obisCode);
                cmd.Parameters.AddWithValue("@name", friendlyName);
                cmd.Parameters.AddWithValue("@cat", category);

                var newId = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<DateTime?> GetLastReadWatermarkUtcAsync(int deviceId, int profileId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT LastReadTimestampUtc FROM DeviceProfileSyncState
                                WHERE DeviceId = @did AND ProfileId = @pid";
            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);

            var val = await cmd.ExecuteScalarAsync();
            if (val != null && val != DBNull.Value)
            {
                return Convert.ToDateTime(val);
            }
            return null;
        }

        private async Task<Dictionary<int, int>> EnsureParametersAsync(int profileId, IReadOnlyList<ProfileColumnInfo> columns)
        {
            var map = new Dictionary<int, int>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Load existing parameters for this profile
            var existingParams = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, ObisCode FROM Parameters WHERE ProfileId = @pid";
                cmd.Parameters.AddWithValue("@pid", profileId);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    int pId = rdr.GetInt32(0);
                    string? obis = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                    if (!string.IsNullOrEmpty(obis) && !existingParams.ContainsKey(obis))
                    {
                        existingParams[obis] = pId;
                    }
                }
            }

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                string obis = !string.IsNullOrEmpty(col.LogicalName) ? col.LogicalName : $"Col_{col.Index}";

                if (existingParams.TryGetValue(obis, out int paramId))
                {
                    map[i] = paramId;
                }
                else
                {
                    // Create missing Parameter
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO Parameters (ProfileId, Name, ObisCode, ObjectType, AttributeIndex, IsHistorical, IsVisible, IsActive, IsDeleted, CreatedDate, CreatedAt, TypeName)
                                        VALUES (@pid, @name, @obis, @objType, @attrIdx, 1, 1, 1, 0, GETUTCDATE(), GETUTCDATE(), 'ABT');
                                        SELECT SCOPE_IDENTITY();";
                    cmd.Parameters.AddWithValue("@pid", profileId);
                    cmd.Parameters.AddWithValue("@name", obis);
                    cmd.Parameters.AddWithValue("@obis", obis);
                    cmd.Parameters.AddWithValue("@objType", (object?)col.ObjectType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@attrIdx", col.AttributeIndex);

                    var newId = await cmd.ExecuteScalarAsync();
                    int newParamId = Convert.ToInt32(newId);
                    existingParams[obis] = newParamId;
                    map[i] = newParamId;
                }
            }

            return map;
        }

        private async Task<HashSet<DateTime>> GetExistingEntryTimestampsUtcAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId)
        {
            var set = new HashSet<DateTime>();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT EntryTimestampUtc FROM ReadingSessions
                                WHERE DeviceId = @did AND ProfileId = @pid AND EntryTimestampUtc IS NOT NULL";
            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);

            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                set.Add(rdr.GetDateTime(0));
            }

            return set;
        }

        private async Task<long> InsertReadingSessionAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId, DateTime readTime, DateTime? entryTimestampUtc)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO ReadingSessions (DeviceId, ProfileId, ReadTime, EntryTimestampUtc)
                                VALUES (@did, @pid, @rt, @et);
                                SELECT SCOPE_IDENTITY();";
            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);
            cmd.Parameters.AddWithValue("@rt", readTime);
            cmd.Parameters.AddWithValue("@et", (object?)entryTimestampUtc ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(id);
        }

        private async Task InsertReadingValueAsync(SqlConnection conn, SqlTransaction tx, long sessionId, int parameterId, string value, string? rawValue, double? numericValue)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO ReadingValues (SessionId, ParameterId, Value, RawValue, ValueNumeric)
                                VALUES (@sid, @pid, @val, @raw, @num)";
            cmd.Parameters.AddWithValue("@sid", sessionId);
            cmd.Parameters.AddWithValue("@pid", parameterId);
            cmd.Parameters.AddWithValue("@val", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@raw", (object?)rawValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@num", (object?)numericValue ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpsertDeviceLatestReadingAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int parameterId, string value, string? rawValue, DateTime updatedAt)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                MERGE DeviceLatestReadings AS target
                USING (SELECT @did AS DeviceId, @pid AS ParameterId) AS source
                ON (target.DeviceId = source.DeviceId AND target.ParameterId = source.ParameterId)
                WHEN MATCHED THEN
                    UPDATE SET target.Value = @val, target.RawValue = @raw, target.UpdatedAt = @updated
                WHEN NOT MATCHED THEN
                    INSERT (DeviceId, ParameterId, Value, RawValue, UpdatedAt)
                    VALUES (@did, @pid, @val, @raw, @updated);";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", parameterId);
            cmd.Parameters.AddWithValue("@val", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@raw", (object?)rawValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updated", updatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpsertDeviceProfileSyncStateAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId, DateTime lastReadTimestampUtc, DateTime lastSyncedAt)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                MERGE DeviceProfileSyncState AS target
                USING (SELECT @did AS DeviceId, @pid AS ProfileId) AS source
                ON (target.DeviceId = source.DeviceId AND target.ProfileId = source.ProfileId)
                WHEN MATCHED THEN
                    UPDATE SET target.LastReadTimestampUtc = @lr, target.LastSyncedAt = @ls
                WHEN NOT MATCHED THEN
                    INSERT (DeviceId, ProfileId, LastReadTimestampUtc, LastSyncedAt)
                    VALUES (@did, @pid, @lr, @ls);";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);
            cmd.Parameters.AddWithValue("@lr", lastReadTimestampUtc);
            cmd.Parameters.AddWithValue("@ls", lastSyncedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        private static double? TryParseDouble(string input)
        {
            if (double.TryParse(input, out var val)) return val;
            return null;
        }
    }
}
