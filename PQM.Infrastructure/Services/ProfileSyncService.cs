using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.Events;

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
        private readonly IEventPublisher _eventPublisher;
        private const int TimeSeriesBatchSize = 100;
        private const int LastSyncProfileId = 15;
        public ProfileSyncService(string connectionString, ILogger<ProfileSyncService> logger, IEventPublisher eventPublisher)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
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
            var result = new DeviceSyncResult
            {
                DeviceId = deviceId
            };

            if (!TryAcquireLock(deviceId))
            {
                result.ErrorMessage = $"Sync already in progress for device {deviceId}.";
                result.AlreadyInProgress = true;
                return result;
            }

            // This is the time when the sync operation started.
            // It is NOT the meter reading timestamp.
            DateTime syncExecutionTimeIST =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            // This will contain the latest meter-data timestamp
            // successfully saved from all profiles.

            using var hardCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

                await using var reader =
                    new DlmsMeterReader(device, verboseLogging: false);

                try
                {
                    await reader.ConnectAsync(syncToken);

                    await reader.ReadAssociationViewAsync(syncToken);

                    foreach (var kvp in ProfileCatalog.AllProfiles)
                    {
                        syncToken.ThrowIfCancellationRequested();

                        string obisCode = kvp.Key;

                        result.ProfilesAttempted++;

                        try
                        {
                            var profileResult =
                                await SyncSingleProfileOnOpenReaderAsync(
                                    reader,
                                    device,
                                    obisCode,
                                    syncExecutionTimeIST,
                                    syncToken);

                            result.ProfileResults[obisCode] = profileResult;

                            if (profileResult.Success)
                            {
                                result.ProfilesSucceeded++;

                                result.TotalRowsRead += profileResult.RowsRead;
                                result.TotalRowsWritten += profileResult.RowsWritten;
                                result.TotalRowsSkipped += profileResult.RowsSkipped;
                            }
                        }
                        catch (OperationCanceledException)
                            when (syncToken.IsCancellationRequested)
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
                catch (OperationCanceledException)
                    when (syncToken.IsCancellationRequested)
                {
                    result.ErrorMessage = "Sync timed out after 90 minutes.";
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Connection failure: {ex.Message}";
                }


                return result;
            }
            catch (OperationCanceledException) when (syncToken.IsCancellationRequested)
            {
                result.ErrorMessage = "Sync timed out after 90 minutes.";
                return result;
            }
            finally
            {
                ReleaseLock(deviceId);
            }
        }
        private async Task<SyncResult> SyncSingleProfileOnOpenReaderAsync(DlmsMeterReader reader, Device device, string obisCode, DateTime syncExecutionTimeIST, CancellationToken cancellationToken = default)
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

            var knownMeta = await GetKnownParameterMetaAsync(profileId);

            var profileObj = reader.GetProfileObjects().FirstOrDefault(p => p.LogicalName == obisCode);
            IReadOnlyList<ProfileColumnInfo> columns = profileObj != null
                ? await reader.ReadCaptureObjectsAsync(profileObj, knownMeta, cancellationToken)
                : new List<ProfileColumnInfo>();

            var parameterMap = await EnsureParametersAsync(profileId, columns);

            var rows = await reader.ReadProfileAllEntriesAsync(
                obisCode, currentWatermarkIST, lastEntriesInUse, cancellationToken);

            result.RowsRead = rows.Count;

            uint? newEntriesInUse = reader.GetProfileEntriesInUse(obisCode);

            if (rows.Count == 0)
            {
                result.Success = true;
                return result;
            }

            return await SaveReadingSessionAsync(
                device.Id, profileId, obisCode, isTimeSeries,
                rows, columns, parameterMap,
                currentWatermarkIST, syncExecutionTimeIST,
                newEntriesInUse);
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
        private async Task<int> GetOrCreateParameterForColumnAsync(SqlConnection conn, SqlTransaction tx, int profileId, int colIndex, IReadOnlyList<ProfileColumnInfo> columns)
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
        private async Task<HashSet<DateTime>> GetExistingEntryTimestampsIST(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId)
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
        private async Task<long> InsertReadingSessionAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId, DateTime readTimeIST, DateTime? entryTimestampIST)
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
        private async Task InsertReadingValueAsync(SqlConnection conn, SqlTransaction tx, long sessionId, int parameterId, string value, string? rawValue, double? numericValue)
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
        private async Task UpsertDeviceProfileSyncStateAsync(SqlConnection conn, SqlTransaction tx, int deviceId, int profileId, DateTime lastReadTimestampIST, DateTime lastSyncedAtIST, int? lastEntriesInUse)
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
        private async Task<SyncResult> SaveReadingSessionAsync(int deviceId, int profileId, string obisCode, bool isTimeSeries, IReadOnlyList<ProfileRow> rows, IReadOnlyList<ProfileColumnInfo> columns, Dictionary<int, int> parameterMap, DateTime? currentWatermarkIST, DateTime syncExecutionTimeIST, uint? newEntriesInUse)
        {
            if (rows.Count == 0)
                return new SyncResult { Success = true };

            // Static/metadata profiles: unchanged — one transaction, no batching.
            if (!isTimeSeries)
            {
                return await SaveBatchAsync(
                    deviceId, profileId, isTimeSeries, rows, columns, parameterMap,
                    currentWatermarkIST, syncExecutionTimeIST, newEntriesInUse,
                    sharedExistingTimestamps: null);
            }

            // Time-series profiles: split into 500-row batches, each its own transaction.
            // Time-series profiles: fetch duplicates ONCE, split into 500-row batches
            var existingTimestamps = await GetExistingEntryTimestampsIST(deviceId, profileId);

            var aggregate = new SyncResult { RowsRead = rows.Count };
            DateTime? runningWatermark = currentWatermarkIST;

            for (int offset = 0; offset < rows.Count; offset += TimeSeriesBatchSize)
            {
                var batchRows = rows.Skip(offset).Take(TimeSeriesBatchSize).ToList();

                var batchResult = await SaveBatchAsync(
                    deviceId, profileId, isTimeSeries, batchRows, columns, parameterMap,
                    runningWatermark, syncExecutionTimeIST, newEntriesInUse,
                    existingTimestamps);  // ← Pass shared set, not null

                aggregate.RowsWritten += batchResult.RowsWritten;
                aggregate.RowsSkipped += batchResult.RowsSkipped;

                if (!batchResult.Success)
                {
                    aggregate.Success = false;
                    aggregate.NewWatermarkIST = runningWatermark;
                    aggregate.ErrorMessage = batchResult.ErrorMessage;
                    return aggregate;
                }

                if (batchResult.NewWatermarkIST.HasValue)
                    runningWatermark = batchResult.NewWatermarkIST;
            }

            aggregate.Success = true;
            aggregate.NewWatermarkIST = runningWatermark;
            return aggregate;
        }
        private async Task<SyncResult> SaveBatchAsync(int deviceId, int profileId, bool isTimeSeries, IReadOnlyList<ProfileRow> batchRows, IReadOnlyList<ProfileColumnInfo> columns, Dictionary<int, int> parameterMap, DateTime? currentWatermarkIST, DateTime syncExecutionTimeIST, uint? newEntriesInUse, HashSet<DateTime>? sharedExistingTimestamps)
        {
            var result = new SyncResult { RowsRead = batchRows.Count };
            DateTime? maxWrittenEntryIST = null;
            DateTime? maxLastSyncCandidate = null;   // NEW

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // Static profiles still fetch duplicates inside their own transaction, exactly
                // as before. Time-series profiles reuse the shared set passed in.
                var existingTimestamps = sharedExistingTimestamps
                    ?? await GetExistingEntryTimestampsIST(conn, tx, deviceId, profileId);

                var newlyWritten = new List<DateTime>();
                var lastSyncUpdates = new List<DateTime>();   // NEW

                foreach (var row in batchRows)
                {
                    DateTime? entryTimestampIST =
                        row.Timestamp.HasValue && row.Timestamp.Value.Year > 1
                            ? row.Timestamp.Value
                            : null;

                    if (entryTimestampIST.HasValue && entryTimestampIST.Value > syncExecutionTimeIST.AddMinutes(5))
                    {
                        Console.WriteLine(
                            $"[BAD TIMESTAMP] DeviceId={deviceId} ProfileId={profileId} " +
                            $"Row timestamp {entryTimestampIST:yyyy-MM-dd HH:mm:ss} is after " +
                            $"sync time {syncExecutionTimeIST:yyyy-MM-dd HH:mm:ss}. Skipping row.");
                        result.RowsSkipped++;
                        continue;
                    }

                    if (entryTimestampIST.HasValue && existingTimestamps.Contains(entryTimestampIST.Value))
                    {
                        result.RowsSkipped++;
                        continue;
                    }

                    long sessionId = await InsertReadingSessionAsync(conn, tx, deviceId, profileId, syncExecutionTimeIST, entryTimestampIST);

                    // NEW
                    if (entryTimestampIST.HasValue && profileId == LastSyncProfileId)
                    {
                        if (!maxLastSyncCandidate.HasValue || entryTimestampIST.Value > maxLastSyncCandidate.Value) maxLastSyncCandidate = entryTimestampIST.Value;
                    }

                    for (int i = 0; i < row.Values.Count; i++)
                    {
                        int parameterId;

                        if (!parameterMap.TryGetValue(i, out parameterId) || parameterId <= 0)
                        {
                            parameterId = await GetOrCreateParameterForColumnAsync(conn, tx, profileId, i, columns);
                            parameterMap[i] = parameterId;
                        }

                        var value = row.Values[i];
                        string formattedValue = ValueFormatter.FormatValue(value);
                        string cleanedValue = ValueFormatter.CleanValue(formattedValue);

                        await InsertReadingValueAsync(
                            conn, tx, sessionId, parameterId,
                            cleanedValue, value?.ToString(), TryParseDouble(cleanedValue));
                    }

                    result.RowsWritten++;

                    if (entryTimestampIST.HasValue)
                    {
                        newlyWritten.Add(entryTimestampIST.Value);

                        if (!maxWrittenEntryIST.HasValue || entryTimestampIST.Value > maxWrittenEntryIST.Value)
                            maxWrittenEntryIST = entryTimestampIST.Value;
                    }
                }
                if (result.RowsWritten > 0)
                {
                    DateTime? watermark = maxWrittenEntryIST ?? currentWatermarkIST ?? syncExecutionTimeIST;

                    if (watermark.HasValue && watermark.Value > syncExecutionTimeIST)
                        watermark = syncExecutionTimeIST;

                    if (watermark.HasValue)
                    {
                        // Only update sync state table for TIME-SERIES profiles
                        if (isTimeSeries)
                        {
                            await UpsertDeviceProfileSyncStateAsync(conn, tx, deviceId, profileId, watermark.Value, syncExecutionTimeIST, (int?)newEntriesInUse);
                        }

                        // ✅ NOW set NewWatermarkIST for ALL profiles (static + time-series)
                        result.NewWatermarkIST = watermark;
                    }
                }

                bool lastSyncAdvanced = false;
                if (maxLastSyncCandidate.HasValue)
                {
                    lastSyncAdvanced = await UpdateDeviceLastSyncInTxAsync(conn, tx, deviceId, maxLastSyncCandidate.Value);
                }

                await tx.CommitAsync();

                if (lastSyncAdvanced && maxLastSyncCandidate.HasValue)
                {
                    await _eventPublisher.PublishAsync(new DeviceSyncCompletedEvent
                    {
                        DeviceId = deviceId,
                        LastSyncAt = maxLastSyncCandidate.Value
                    });
                }

                // Only mark rows as "known" once this batch is actually committed —
                // if we added them before commit, a rolled-back batch would poison
                // the shared duplicate-check set for later batches.
                if (sharedExistingTimestamps != null)
                    foreach (var ts in newlyWritten)
                        sharedExistingTimestamps.Add(ts);

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
        private async Task<HashSet<DateTime>> GetExistingEntryTimestampsIST(int deviceId, int profileId)
        {
            var result = new HashSet<DateTime>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
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
                result.Add(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Unspecified));

            return result;
        }
        private static double? TryParseDouble(string input) => double.TryParse(input, out var value) ? value : null;
        private async Task<bool> UpdateDeviceLastSyncInTxAsync(SqlConnection conn, SqlTransaction tx, int deviceId, DateTime entryTimestampIST)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
        UPDATE Devices
        SET LastSyncAt = @ts
        WHERE Id = @id
          AND (LastSyncAt IS NULL OR LastSyncAt < @ts)";

            cmd.Parameters.Add("@ts", System.Data.SqlDbType.DateTime2).Value = entryTimestampIST;
            cmd.Parameters.Add("@id", System.Data.SqlDbType.Int).Value = deviceId;

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;   // NEW — true only if it actually moved LastSyncAt forward
        }
        private async Task<Dictionary<string, (int? Scaler, int? UnitCode, string? Unit)>> GetKnownParameterMetaAsync(int profileId)
        {
            var map = new Dictionary<string, (int?, int?, string?)>(StringComparer.OrdinalIgnoreCase);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT ObisCode, Scaler, UnitCode, Unit
        FROM Parameters
        WHERE ProfileId = @pid AND ObisCode IS NOT NULL";
            cmd.Parameters.AddWithValue("@pid", profileId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                map[reader.GetString(0)] = (
                    reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3));
            }

            return map;
        }
    }
}