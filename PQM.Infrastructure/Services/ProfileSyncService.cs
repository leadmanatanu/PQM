using Microsoft.Data.SqlClient;
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
        public DateTime? NewWatermarkIST { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DeviceSyncResult
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool AlreadyInProgress { get; set; }
        public int ProfilesAttempted { get; set; }
        public int ProfilesSucceeded { get; set; }
        public int TotalRowsRead { get; set; }
        public int TotalRowsWritten { get; set; }
        public int TotalRowsSkipped { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, SyncResult> ProfileResults { get; set; } = new();
    }

    public class ProfileSyncService
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> _activeDeviceSyncs = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DateTime> _lockAcquiredTimes = new();

        private readonly string _connectionString;
        private readonly ILogger<ProfileSyncService> _logger;

        public ProfileSyncService(string connectionString, ILogger<ProfileSyncService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static bool TryAcquireLock(int deviceId)
        {
            if (_lockAcquiredTimes.TryGetValue(deviceId, out var acquiredAt) &&
                DateTime.UtcNow - acquiredAt > TimeSpan.FromMinutes(90))
            {
                _activeDeviceSyncs.TryRemove(deviceId, out _);
                _lockAcquiredTimes.TryRemove(deviceId, out _);
            }

            if (_activeDeviceSyncs.TryAdd(deviceId, 1))
            {
                _lockAcquiredTimes[deviceId] = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public static void ReleaseLock(int deviceId)
        {
            _activeDeviceSyncs.TryRemove(deviceId, out _);
            _lockAcquiredTimes.TryRemove(deviceId, out _);
        }

        public async Task<DeviceSyncResult> SyncDeviceAllProfilesAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var result = new DeviceSyncResult { DeviceId = deviceId };

            if (!TryAcquireLock(deviceId))
            {
                result.ErrorMessage = $"Sync already in progress for device {deviceId}.";
                result.AlreadyInProgress = true;
                return result;
            }

            DateTime syncExecutionTimeIST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            using var hardCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            hardCts.CancelAfter(TimeSpan.FromMinutes(90));
            var syncToken = hardCts.Token;

            try
            {
                var device = await LoadDeviceAsync(deviceId);

                if (device == null)
                {
                    result.ErrorMessage = $"Device with Id={deviceId} not found.";
                    return result;
                }

                result.DeviceName = device.Name;

                await using var reader = new DlmsMeterReader(device, verboseLogging: false);

                try
                {
                    await reader.ConnectAsync(syncToken);
                    await reader.ReadAssociationViewAsync(syncToken);

                    foreach (var kvp in ProfileCatalog.AllProfiles)
                    {
                        syncToken.ThrowIfCancellationRequested();

                        string obisCode = kvp.Key;
                        result.ProfilesAttempted++;

                        using var profileCts = CancellationTokenSource.CreateLinkedTokenSource(syncToken);

                        TimeSpan timeout = obisCode switch
                        {
                            "1.0.99.1.0.255" => TimeSpan.FromMinutes(30),
                            "1.0.99.2.0.255" => TimeSpan.FromMinutes(10),
                            _ => TimeSpan.FromMinutes(5)
                        };

                        profileCts.CancelAfter(timeout);

                        try
                        {
                            var profileResult = await SyncSingleProfileOnOpenReaderAsync(reader, device, obisCode, syncExecutionTimeIST, profileCts.Token);

                            result.ProfileResults[obisCode] = profileResult;

                            if (profileResult.Success)
                            {
                                result.ProfilesSucceeded++;
                                result.TotalRowsRead += profileResult.RowsRead;
                                result.TotalRowsWritten += profileResult.RowsWritten;
                                result.TotalRowsSkipped += profileResult.RowsSkipped;
                            }
                        }
                        catch (OperationCanceledException) when (profileCts.IsCancellationRequested && !syncToken.IsCancellationRequested)
                        {
                            result.ProfileResults[obisCode] = new SyncResult
                            {
                                Success = false,
                                ErrorMessage = $"Profile read timed out after {Math.Round(timeout.TotalMinutes)} minutes"
                            };
                        }
                        catch (OperationCanceledException) when (syncToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            result.ProfileResults[obisCode] = new SyncResult
                            {
                                Success = false,
                                ErrorMessage = ex.Message
                            };
                        }
                    }

                    result.Success = result.ProfilesSucceeded > 0;
                }
                catch (OperationCanceledException) when (syncToken.IsCancellationRequested)
                {
                    result.ErrorMessage = "Sync timed out after 90 minutes";
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Connection failure: {ex.Message}";
                }

                await UpdateDeviceStatusInDbAsync(deviceId, syncExecutionTimeIST);
                return result;
            }
            catch (OperationCanceledException) when (syncToken.IsCancellationRequested)
            {
                result.ErrorMessage = "Sync timed out after 90 minutes";
                await UpdateDeviceStatusInDbAsync(deviceId, syncExecutionTimeIST);
                return result;
            }
            finally
            {
                ReleaseLock(deviceId);
            }
        }

        private async Task<SyncResult> SyncSingleProfileOnOpenReaderAsync(
            DlmsMeterReader reader,
            Device device,
            string obisCode,
            DateTime syncExecutionTimeIST,
            CancellationToken cancellationToken = default)
        {
            var result = new SyncResult();
            bool isTimeSeries = ProfileCatalog.TimeSeriesProfiles.ContainsKey(obisCode);
            int profileId = await EnsureProfileAsync(obisCode, isTimeSeries);

            DateTime? currentWatermarkIST = null;
            int? lastEntriesInUse = null;

            if (isTimeSeries)
            {
                var state = await GetLastReadWatermarkIST(device.Id, profileId);
                currentWatermarkIST = state.WatermarkIST;
                lastEntriesInUse = state.LastEntriesInUse;
            }

            var profileObj = reader.GetProfileObjects().FirstOrDefault(p => p.LogicalName == obisCode);
            IReadOnlyList<ProfileColumnInfo> columns = profileObj != null
                ? await reader.ReadCaptureObjectsAsync(profileObj, cancellationToken)
                : new List<ProfileColumnInfo>();

            var parameterMap = await EnsureParametersAsync(profileId, columns);

            var rows = await reader.ReadProfileAllEntriesAsync(
                obisCode, currentWatermarkIST, lastEntriesInUse, cancellationToken); // pass lastEntriesInUse

            result.RowsRead = rows.Count;

            uint? newEntriesInUse = reader.GetProfileEntriesInUse(obisCode); // NEW — read after fetch

            if (rows.Count == 0)
            {
                result.Success = true;
                return result;
            }

            return await SaveReadingSessionAsync(
                device.Id, profileId, obisCode, isTimeSeries,
                rows, columns, parameterMap,
                currentWatermarkIST, syncExecutionTimeIST,
                newEntriesInUse);   // NEW — pass through
        }

        private async Task<SyncResult> SaveReadingSessionAsync(
            int deviceId,
            int profileId,
            string obisCode,
            bool isTimeSeries,
            IReadOnlyList<ProfileRow> rows,
            IReadOnlyList<ProfileColumnInfo> columns,
            Dictionary<int, int> parameterMap,
            DateTime? currentWatermarkIST,
            DateTime syncExecutionTimeIST,
            uint? newEntriesInUse)
        {
            var result = new SyncResult { RowsRead = rows.Count };

            if (rows.Count == 0)
            {
                result.Success = true;
                return result;
            }

            DateTime? maxWrittenEntryIST = null;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var existingTimestamps = await GetExistingEntryTimestampsIST(
                    conn, tx, deviceId, profileId);

                foreach (var row in rows)
                {
                    DateTime? entryTimestampIST =
                        row.Timestamp.HasValue && row.Timestamp.Value.Year > 1
                            ? row.Timestamp.Value
                            : null;

                    // NEW: reject rows whose timestamp is in the future relative to
                    // this sync's actual execution time. This catches meter-clock drift
                    // (the meter's RTC running ahead of real time) before it corrupts
                    // the readings table.
                    if (entryTimestampIST.HasValue && entryTimestampIST.Value > syncExecutionTimeIST.AddMinutes(5))
                    {
                        Console.WriteLine(
                            $"[BAD TIMESTAMP] DeviceId={deviceId} ProfileId={profileId} " +
                            $"Row timestamp {entryTimestampIST:yyyy-MM-dd HH:mm:ss} is after " +
                            $"sync time {syncExecutionTimeIST:yyyy-MM-dd HH:mm:ss}. Skipping row."
                        );
                        result.RowsSkipped++;
                        continue;
                    }

                    if (entryTimestampIST.HasValue &&
                        existingTimestamps.Contains(entryTimestampIST.Value))
                    {
                        result.RowsSkipped++;
                        continue;
                    }

                    long sessionId = await InsertReadingSessionAsync(
                        conn, tx, deviceId, profileId,
                        syncExecutionTimeIST, entryTimestampIST);

                    for (int i = 0; i < row.Values.Count; i++)
                    {
                        int parameterId;

                        if (!parameterMap.TryGetValue(i, out parameterId) || parameterId <= 0)
                        {
                            parameterId = await GetOrCreateParameterForColumnAsync(
                                conn, tx, profileId, i, columns);
                            parameterMap[i] = parameterId;
                        }

                        var value = row.Values[i];

                        string formattedValue = ValueFormatter.FormatValue(value);
                        string cleanedValue = ValueFormatter.CleanValue(formattedValue);
                        Console.WriteLine(
                            $"[DB VALUE DEBUG] " +
                            $"Index={i} | " +
                            $"ParameterId={parameterId} | " +
                            $"ColumnOBIS={(i < columns.Count ? columns[i].LogicalName : "N/A")} | " +
                            $"Unit={(i < columns.Count ? columns[i].Unit : "N/A")} | " +
                            $"Type={value?.GetType().FullName ?? "null"} | " +
                            $"Original={value ?? "null"} | " +
                            $"Formatted={formattedValue} | " +
                            $"Cleaned={cleanedValue}"
                        );

                        //await InsertReadingValueAsync(
                        //    conn, tx, sessionId, parameterId,
                        //    formattedValue,
                        //    value?.ToString(),
                        //    TryParseDouble(formattedValue));

                        await InsertReadingValueAsync(
                            conn, tx, sessionId, parameterId,
                            cleanedValue,                // ? Pass already-cleaned
                            value?.ToString(),
                            TryParseDouble(cleanedValue));   // ? Use already-cleaned
                    }

                    result.RowsWritten++;

                    if (entryTimestampIST.HasValue)
                    {
                        existingTimestamps.Add(entryTimestampIST.Value);

                        if (!maxWrittenEntryIST.HasValue ||
                            entryTimestampIST.Value > maxWrittenEntryIST.Value)
                            maxWrittenEntryIST = entryTimestampIST.Value;
                    }
                }

                if (isTimeSeries)
                {
                    DateTime? watermark = maxWrittenEntryIST ?? currentWatermarkIST;

                    if (watermark.HasValue && watermark.Value > syncExecutionTimeIST)
                        watermark = syncExecutionTimeIST;

                    if (watermark.HasValue)
                    {
                        await UpsertDeviceProfileSyncStateAsync(conn, tx, deviceId, profileId,watermark.Value, syncExecutionTimeIST,(int?)newEntriesInUse);  

                        result.NewWatermarkIST = watermark;
                    }
                }

                await tx.CommitAsync();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                result.ErrorMessage = $"Database transaction error: {ex.Message}";
                return result;
            }
        }

        private async Task UpdateDeviceStatusInDbAsync(int deviceId, DateTime lastSyncIST)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Devices SET LastSyncAt = @lastSync WHERE Id = @id";
            cmd.Parameters.AddWithValue("@lastSync", lastSyncIST);
            cmd.Parameters.AddWithValue("@id", deviceId);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<Device?> LoadDeviceAsync(int deviceId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, IP, PORT, ClientAddress, ServerAddress,
                       AuthenticationTypeId, Password, Timeout, TimeZoneId
                FROM Devices WHERE Id = @id";

            cmd.Parameters.AddWithValue("@id", deviceId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Device
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                IP = reader.GetString(reader.GetOrdinal("IP")),
                PORT = reader.GetInt32(reader.GetOrdinal("PORT")),
                ClientAddress = reader.IsDBNull(reader.GetOrdinal("ClientAddress"))
                    ? 16 : reader.GetInt32(reader.GetOrdinal("ClientAddress")),
                ServerAddress = reader.IsDBNull(reader.GetOrdinal("ServerAddress"))
                    ? 1 : reader.GetInt32(reader.GetOrdinal("ServerAddress")),
                AuthenticationTypeId = reader.IsDBNull(reader.GetOrdinal("AuthenticationTypeId"))
                    ? null : reader.GetInt32(reader.GetOrdinal("AuthenticationTypeId")),
                Password = reader.IsDBNull(reader.GetOrdinal("Password"))
                    ? null : reader.GetString(reader.GetOrdinal("Password")),
                Timeout = reader.IsDBNull(reader.GetOrdinal("Timeout"))
                    ? 30000 : reader.GetInt32(reader.GetOrdinal("Timeout")),
                TimeZoneId = reader.IsDBNull(reader.GetOrdinal("TimeZoneId"))
                    ? null : reader.GetString(reader.GetOrdinal("TimeZoneId"))
            };
        }

        private async Task<int> EnsureProfileAsync(string obisCode, bool isTimeSeries)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Profiles WHERE ObisCode = @obis";
            cmd.Parameters.AddWithValue("@obis", obisCode);

            var id = await cmd.ExecuteScalarAsync();
            if (id != null && id != DBNull.Value)
                return Convert.ToInt32(id);

            cmd.CommandText = @"
                INSERT INTO Profiles (ObisCode, FriendlyName, Category)
                VALUES (@obis, @name, @cat);
                SELECT SCOPE_IDENTITY();";

            cmd.Parameters.AddWithValue("@name",
                ProfileCatalog.AllProfiles.GetValueOrDefault(obisCode, obisCode));
            cmd.Parameters.AddWithValue("@cat", isTimeSeries ? "TimeSeries" : "Static");

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<(DateTime? WatermarkIST, int? LastEntriesInUse)> GetLastReadWatermarkIST(int deviceId, int profileId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT LastReadTimestamp, LastEntriesInUse
        FROM DeviceProfileSyncState
        WHERE DeviceId = @did AND ProfileId = @pid";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return (null, null);

            DateTime? watermark = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
            int? lastEntries = reader.IsDBNull(1) ? null : reader.GetInt32(1);

            return (watermark, lastEntries);
        }

        private async Task<Dictionary<int, int>> EnsureParametersAsync(
            int profileId,
            IReadOnlyList<ProfileColumnInfo> columns)
        {
            var map = new Dictionary<int, int>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var existing = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT Id, ObisCode FROM Parameters WHERE ProfileId = @pid";
                cmd.Parameters.AddWithValue("@pid", profileId);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(1))
                        existing[reader.GetString(1)] = reader.GetInt32(0);
                }
            }

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                string obis = !string.IsNullOrEmpty(col.LogicalName)
                    ? col.LogicalName
                    : $"Col_{col.Index}";

                if (existing.TryGetValue(obis, out int id))
                {
                    map[i] = id;
                    continue;
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Parameters
                    (ProfileId, Name, ObisCode, ObjectType, AttributeIndex,
                     Scaler, UnitCode, Unit, IsHistorical, IsVisible, CreatedAt)
                    VALUES
                    (@pid, @name, @obis, @objType, @attrIdx,
                     @scaler, @unitCode, @unit, 1, 1, GETUTCDATE());
                    SELECT SCOPE_IDENTITY();";

                cmd.Parameters.AddWithValue("@pid", profileId);
                cmd.Parameters.AddWithValue("@name", obis);
                cmd.Parameters.AddWithValue("@obis", obis);
                cmd.Parameters.AddWithValue("@objType", (object?)col.ObjectType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@attrIdx", col.AttributeIndex);
                cmd.Parameters.AddWithValue("@scaler", (object?)col.Scaler ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unitCode", (object?)col.UnitCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object?)col.Unit ?? DBNull.Value);

                int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                existing[obis] = newId;
                map[i] = newId;
            }

            return map;
        }

        private async Task<int> GetOrCreateParameterForColumnAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int profileId,
            int colIndex,
            IReadOnlyList<ProfileColumnInfo> columns)
        {
            string obis = colIndex < columns.Count &&
                          !string.IsNullOrEmpty(columns[colIndex].LogicalName)
                ? columns[colIndex].LogicalName
                : $"Param_{profileId}_{colIndex}";

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT Id FROM Parameters WHERE ProfileId = @pid AND ObisCode = @obis";
            cmd.Parameters.AddWithValue("@pid", profileId);
            cmd.Parameters.AddWithValue("@obis", obis);

            var existing = await cmd.ExecuteScalarAsync();

            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            cmd.CommandText = @"
                INSERT INTO Parameters
                (ProfileId, Name, ObisCode, AttributeIndex, IsHistorical, IsVisible, CreatedAt)
                VALUES (@pid, @name, @obis, 2, 1, 1, GETUTCDATE());
                SELECT SCOPE_IDENTITY();";

            cmd.Parameters.AddWithValue("@name",
                colIndex < columns.Count &&
                !string.IsNullOrEmpty(columns[colIndex].Description)
                    ? columns[colIndex].Description
                    : obis);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<HashSet<DateTime>> GetExistingEntryTimestampsIST(
            SqlConnection conn,
            SqlTransaction tx,
            int deviceId,
            int profileId)
        {
            var result = new HashSet<DateTime>();

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                SELECT EntryTimestamp
                FROM ReadingSessions
                WHERE DeviceId = @did
                  AND ProfileId = @pid
                  AND EntryTimestamp IS NOT NULL";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                result.Add(DateTime.SpecifyKind(
                    reader.GetDateTime(0),
                    DateTimeKind.Unspecified));

            return result;
        }

        private async Task<long> InsertReadingSessionAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int deviceId,
            int profileId,
            DateTime readTimeIST,
            DateTime? entryTimestampIST)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO ReadingSessions
                (DeviceId, ProfileId, ReadTimeAt, EntryTimestamp)
                VALUES (@did, @pid, @rt, @et);
                SELECT SCOPE_IDENTITY();";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);
            cmd.Parameters.AddWithValue("@rt", readTimeIST);
            cmd.Parameters.AddWithValue("@et", (object?)entryTimestampIST ?? DBNull.Value);

            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        private async Task InsertReadingValueAsync(
            SqlConnection conn,
            SqlTransaction tx,
            long sessionId,
            int parameterId,
            string value,
            string? rawValue,
            double? numericValue)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO ReadingValues
                (SessionId, ParameterId, Value, RawValue, ValueNumeric)
                VALUES (@sid, @pid, @val, @raw, @num)";

            cmd.Parameters.AddWithValue("@sid", sessionId);
            cmd.Parameters.AddWithValue("@pid", parameterId);
            cmd.Parameters.AddWithValue("@val", (object?)ValueFormatter.CleanValue(value) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@raw", (object?)rawValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@num", (object?)numericValue ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpsertDeviceProfileSyncStateAsync(
     SqlConnection conn,
     SqlTransaction tx,
     int deviceId,
     int profileId,
     DateTime lastReadTimestampIST,
     DateTime lastSyncedAtIST,
     int? lastEntriesInUse)   
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
        MERGE DeviceProfileSyncState AS target
        USING (SELECT @did AS DeviceId, @pid AS ProfileId) AS source
        ON target.DeviceId = source.DeviceId
           AND target.ProfileId = source.ProfileId
        WHEN MATCHED THEN
            UPDATE SET LastReadTimestamp = @lr, LastSyncedAt = @ls, LastEntriesInUse = @le
        WHEN NOT MATCHED THEN
            INSERT (DeviceId, ProfileId, LastReadTimestamp, LastSyncedAt, LastEntriesInUse)
            VALUES (@did, @pid, @lr, @ls, @le);";

            cmd.Parameters.AddWithValue("@did", deviceId);
            cmd.Parameters.AddWithValue("@pid", profileId);
            cmd.Parameters.AddWithValue("@lr", lastReadTimestampIST);
            cmd.Parameters.AddWithValue("@ls", lastSyncedAtIST);
            cmd.Parameters.AddWithValue("@le", (object?)lastEntriesInUse ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        private static double? TryParseDouble(string input) =>
            double.TryParse(input, out var value) ? value : null;
    }
}