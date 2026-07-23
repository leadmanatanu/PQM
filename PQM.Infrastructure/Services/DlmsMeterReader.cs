using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gurux.Common;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.Net;
using PQM.Core.Entities;
// Alias required: Gurux.DLMS.Enums also defines a 'Task' type which collides
// with System.Threading.Tasks.Task in this namespace.
using SysTask = System.Threading.Tasks.Task;

namespace PQM.Infrastructure.Services
{
    /// <summary>
    /// Batch/incremental-sync DLMS meter reader.
    ///
    /// This class is the ported and adapted version of the validated prototype at
    /// D:\event_reading\meter_reading. It is intentionally a separate class from
    /// DLMSReader (the interactive/discover reader) for the following reasons:
    ///
    ///   1. Different output model: produces structured ProfileRow objects rather than
    ///      JSON strings, which is what the sync pipeline (Stage 4+) needs.
    ///
    ///   2. AllData = false fix: the critical bug in DLMSReader (AllData = true causes
    ///      Receive() to block for the full WaitTime even after a complete frame arrives)
    ///      is fixed here without touching DLMSReader.cs.
    ///
    ///   3. Incremental sync support: ReadProfileAllEntriesAsync accepts a startTime
    ///      watermark parameter that DLMSReader does not support.
    ///
    ///   4. _isAssociated guard: prevents NullReferenceException from ReleaseRequest()
    ///      when a connection never fully completed association.
    ///
    /// Registration: Transient in DI (stateful per-connection, same pattern as DLMSReader
    /// via DLMSSessionManager — instantiate, use, dispose within one sync operation).
    /// </summary>
    public class DlmsMeterReader : IDisposable, IAsyncDisposable
    {
        private readonly Device _device;
        private readonly GXDLMSClient _client;
        private readonly GXNet _media;
        private readonly bool _verboseLogging;
        private int _lastRequestBytesReceived;

        private bool _connected;

        // Guard flag set to true only after a successful ParseAAREResponse().
        // Used in Disconnect() to avoid calling ReleaseRequest() on a connection
        // that never completed association — which causes a NullReferenceException
        // inside the Gurux library.
        private bool _isAssociated;

        // =========================================================
        // PER-METER SESSION COOLDOWN
        //
        // Some DLMS meters (including this model) do not immediately release
        // their application-layer association after RLRQ. If a new AARQ arrives
        // before the meter's session cleanup completes, the meter responds with
        // "Service Unsupported" (GXDLMSConfirmedServiceError).
        //
        // The cooldown tracker records the last disconnect time for each meter
        // (keyed by IP:PORT). ConnectAsync checks this and waits out any remaining
        // cooldown before attempting Open(). This is transparent in production where
        // syncs are minutes apart, and handles back-to-back syncs (e.g., multiple
        // profiles per device in Stage 5) correctly.
        // =========================================================
        private static readonly ConcurrentDictionary<string, DateTime> _meterLastDisconnect
            = new ConcurrentDictionary<string, DateTime>();

        private const int MeterCooldownSeconds = 35;

        public GXDLMSObjectCollection Objects => _client.Objects;

        // =========================================================
        // CONSTRUCTOR — takes Device entity directly
        // =========================================================

        /// <param name="device">
        ///   The Device entity from PQM.Core. Connection parameters are read directly
        ///   from it: IP, PORT, ClientAddress, ServerAddress, Authentication (via
        ///   AuthenticationTypeId shim), Password, Timeout.
        /// </param>
        /// <param name="verboseLogging">
        ///   When true, traces every sent/received frame byte to Console. Useful for
        ///   diagnosing protocol issues; disable in production.
        /// </param>
        public DlmsMeterReader(Device device, bool verboseLogging = false)
        {
            _device = device;
            _verboseLogging = verboseLogging;
            _connected = false;
            _isAssociated = false;

            // Resolve Authentication enum:
            // If AuthenticationTypeId is set (0=None, 1=Low, etc.), use it.
            // If AuthenticationTypeId is null, check if Password is provided:
            // meters with a Password (e.g. "lnt1") require Low authentication (1).
            Authentication authEnum;
            if (device.AuthenticationTypeId.HasValue && device.AuthenticationTypeId.Value > 0 && Enum.IsDefined(typeof(Authentication), device.AuthenticationTypeId.Value))
            {
                authEnum = (Authentication)device.AuthenticationTypeId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(device.Password))
            {
                authEnum = Authentication.Low;
            }
            else
            {
                authEnum = Authentication.None;
            }

            // ServerAddress: use Device.ServerAddress if set, fall back to 1.
            var serverAddress = (device.ServerAddress ?? 1);

            _client = new GXDLMSClient
            {
                UseLogicalNameReferencing = true,

                ClientAddress = device.ClientAddress ?? 16,

                ServerAddress = serverAddress,

                Authentication = authEnum,

                Password = Encoding.ASCII.GetBytes(device.Password ?? string.Empty),

                // WrapperType (TCP/IP wrapper) must be set explicitly.
                // This is the interface type used by this meter over TCP.
                InterfaceType = InterfaceType.WRAPPER,

                // Conformance flags required for selective access (range/entry reads)
                // and standard Get requests. Keep these exactly as in the prototype —
                // changing them can silently break selective access on some meters.
                ProposedConformance =
                    Conformance.BlockTransferWithGetOrRead | Conformance.Get | Conformance.SelectiveAccess,

                MaxReceivePDUSize = 1024
            };

            _media = new GXNet(
                NetworkType.Tcp,
                device.IP,
                device.PORT);

            // WaitTimeMilliseconds from Device.Timeout (int? property, default 30000).
            _media.WaitTime = device.Timeout ?? 30000;

            if (_verboseLogging)
            {
                _media.Trace = System.Diagnostics.TraceLevel.Verbose;
                _media.OnTrace += (sender, e) =>
                {
                    Console.WriteLine($"[GURUX TRACE] {e}");
                };
            }
        }

        // =========================================================
        // CONNECT
        // =========================================================

        public async SysTask ConnectAsync()
        {
            try
            {
                if (_connected)
                {
                    return;
                }

                // ── PER-METER COOLDOWN ──────────────────────────────────────
                // Enforce minimum gap between sessions on the same meter to prevent
                // "Service Unsupported" AARE rejections during rapid re-connects.
                string meterKey = $"{_device.IP}:{_device.PORT}";
                if (_meterLastDisconnect.TryGetValue(meterKey, out var lastDisconnectUtc))
                {
                    var elapsed = (DateTime.UtcNow - lastDisconnectUtc).TotalSeconds;
                    if (elapsed < MeterCooldownSeconds)
                    {
                        var waitMs = (int)((MeterCooldownSeconds - elapsed) * 1000);
                        Console.WriteLine($"[COOLDOWN] Meter {meterKey} disconnected {elapsed:F0}s ago. Waiting {waitMs}ms before reconnecting...");
                        await System.Threading.Tasks.Task.Delay(waitMs);
                    }
                }
                // Retry media open up to 3 times with 2-second delay between attempts.
                int connectRetries = 3;
                while (connectRetries > 0)
                {
                    try
                    {
                        int openRetries = 3;
                        while (openRetries > 0)
                        {
                            try
                            {
                                _media.Open();
                                break;
                            }
                            catch (Exception ex)
                            {
                                openRetries--;
                                if (openRetries == 0)
                                {
                                    if (_verboseLogging)
                                        Console.WriteLine($"[OPEN ERROR] {ex}");
                                    throw;
                                }
                                await System.Threading.Tasks.Task.Delay(2000);
                            }
                        }

                        var aarqRequests = _client.AARQRequest();

                        if (_verboseLogging)
                        {
                            Console.WriteLine($"[CLIENT STATE] Auth: {_client.Authentication}, PW Length: {_client.Password?.Length ?? 0}, PW Hex: {(_client.Password != null ? BitConverter.ToString(_client.Password) : "null")}");
                            Console.WriteLine($"[AARQ REQUEST] Count: {aarqRequests.Length}");
                            for (int i = 0; i < aarqRequests.Length; i++)
                                Console.WriteLine($"[AARQ REQUEST] [{i}] ({aarqRequests[i].Length} bytes): {BitConverter.ToString(aarqRequests[i])}");
                        }

                        foreach (var request in aarqRequests)
                        {
                            var reply = await SendAndReceiveAsync(request);
                            if (reply.Error != 0)
                                throw new InvalidOperationException($"Meter rejected AARQ. Error Code: {reply.Error}");

                            var buffer = new GXByteBuffer();
                            buffer.Set(reply.Data);
                            _client.ParseAAREResponse(buffer);
                        }

                        break; // Connection succeeded!
                    }
                    catch (Exception ex) when (connectRetries > 1)
                    {
                        connectRetries--;
                        if (_verboseLogging)
                            Console.WriteLine($"[CONNECT RETRY] Connection attempt failed ({ex.Message}), retrying in 5 seconds...");

                        try { _media.Close(); } catch { }
                        await System.Threading.Tasks.Task.Delay(5000);
                    }
                }

                // Association is now confirmed. Only set _isAssociated = true here.
                _isAssociated = true;
                _connected = true;

                Console.WriteLine($"[DlmsMeterReader] Connected to device '{_device.Name}' ({_device.IP}:{_device.PORT}).");
            }
            catch (Exception ex)
            {
                _isAssociated = false;
                _connected = false;
                if (_verboseLogging)
                    Console.WriteLine($"[CONNECT ERROR] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // =========================================================
        // ASSOCIATION VIEW
        // =========================================================

        public async System.Threading.Tasks.Task<IReadOnlyList<string>> ReadAssociationViewAsync()
        {
            EnsureConnected();

            var requests = _client.GetObjectsRequest();
            GXReplyData? finalReply = null;
            int totalBytesReceived = 0;

            foreach (var request in requests)
            {
                finalReply = await SendAndReceiveAsync(request);
                totalBytesReceived += _lastRequestBytesReceived;
            }

            if (finalReply == null)
                throw new InvalidOperationException("No association response received.");

            _client.ParseObjects(finalReply.Data, true);

            Console.WriteLine($"[ASSOCIATION READ] Total bytes received: {totalBytesReceived}");
            Console.WriteLine($"[ASSOCIATION READ] Total objects parsed: {_client.Objects.Count}");

            // Ensure all known profile objects exist in the client's object collection,
            // even if the meter's association view didn't report them explicitly.
            EnsureKnownProfileObjects();

            var result = new List<string>();
            foreach (var obj in _client.Objects)
                result.Add($"{obj.LogicalName} | {obj.GetType().Name}");

            return result;
        }

        /// <summary>
        /// Adds any ProfileCatalog entries that the meter did not return in its
        /// association view. Required because some meters omit certain profiles from
        /// the association view even though they are fully readable.
        /// </summary>
        private void EnsureKnownProfileObjects()
        {
            var addedProfiles = new List<string>();

            foreach (var kvp in ProfileCatalog.AllProfiles)
            {
                var obis = kvp.Key;
                var description = kvp.Value;

                var existing = _client.Objects.FirstOrDefault(o => o.LogicalName == obis);
                if (existing == null)
                {
                    var profile = new GXDLMSProfileGeneric(obis) { Description = description };
                    _client.Objects.Add(profile);
                    addedProfiles.Add($"{obis} ({description})");
                }
            }

            if (addedProfiles.Count > 0)
            {
                Console.WriteLine("[FALLBACK] Manually added missing profile generic objects:");
                foreach (var added in addedProfiles)
                    Console.WriteLine($"  - {added}");
            }
            else
            {
                Console.WriteLine("[FALLBACK] All known profile objects were present in the meter's association view.");
            }
        }

        // =========================================================
        // READ SINGLE OBJECT
        // =========================================================

        public async System.Threading.Tasks.Task<object?> ReadObjectAsync(GXDLMSObject obj, int attributeIndex = 2)
        {
            EnsureConnected();

            var requests = _client.Read(obj, attributeIndex);
            GXReplyData? reply = null;

            foreach (var request in requests)
                reply = await SendAndReceiveAsync(request);

            if (reply == null)
                return null;

            return _client.UpdateValue(obj, attributeIndex, reply.Value);
        }

        // =========================================================
        // GET PROFILE OBJECTS
        // =========================================================

        public List<GXDLMSProfileGeneric> GetProfileObjects()
        {
            return _client.Objects.OfType<GXDLMSProfileGeneric>().ToList();
        }

        // =========================================================
        // READ CAPTURE OBJECTS (attribute 3)
        // =========================================================

        public async System.Threading.Tasks.Task<IReadOnlyList<ProfileColumnInfo>> ReadCaptureObjectsAsync(GXDLMSProfileGeneric profile)
        {
            EnsureConnected();

            // Read CaptureObjects (attribute 3) — populates profile.CaptureObjects
            await ReadObjectAsync(profile, 3);

            // Read EntriesInUse (attribute 7) — needed for ReadRowsByEntry fallback
            try
            {
                await ReadObjectAsync(profile, 7);
            }
            catch
            {
                // Attribute 7 is optional on some meter firmware versions; ignore if absent.
            }

            var result = new List<ProfileColumnInfo>();
            var index = 0;

            foreach (var captureObject in profile.CaptureObjects)
            {
                result.Add(new ProfileColumnInfo
                {
                    Index = index++,
                    LogicalName = captureObject.Key.LogicalName,
                    ObjectType = captureObject.Key.GetType().Name,
                    AttributeIndex = captureObject.Value?.AttributeIndex ?? 2,
                    Description = string.Empty
                });
            }

            return result;
        }

        // =========================================================
        // READ PROFILE ALL ENTRIES
        //
        // Full fallback chain:
        //   1. ReadRowsByRange (selective access by date range — most efficient)
        //   2. ReadRowsByEntry (full buffer by entry index — if range access fails)
        //   3. Raw attribute-2 buffer read (last resort)
        //
        // startTime parameter: used in Stage 4 (incremental sync) to pass the last
        // known watermark so only new rows are retrieved. When null, reads from 2000-01-01
        // (effectively all history). The parameter must be wired through completely
        // correctly here even though Stage 4 has not yet been implemented.
        // =========================================================

        public async System.Threading.Tasks.Task<IReadOnlyList<ProfileRow>> ReadProfileAllEntriesAsync(
            string obisCode,
            DateTime? startTime = null)
        {
            EnsureConnected();

            var profile = _client.Objects
                .OfType<GXDLMSProfileGeneric>()
                .FirstOrDefault(o => o.LogicalName == obisCode);

            if (profile == null)
                throw new InvalidOperationException($"Profile object ({obisCode}) not found in meter objects. Call ReadAssociationViewAsync() first.");

            await ReadCaptureObjectsAsync(profile);

            // --- Attempt 1: Selective access by date range ---
            try
            {
                var start = new GXDateTime(startTime ?? new DateTime(2000, 1, 1));
                var end = new GXDateTime(DateTime.Now);

                start.Skip = DateTimeSkips.Deviation | DateTimeSkips.Status;
                end.Skip = DateTimeSkips.Deviation | DateTimeSkips.Status;

                var requests = _client.ReadRowsByRange(profile, start, end);
                GXReplyData? reply = null;

                foreach (var request in requests)
                    reply = await SendAndReceiveAsync(request);

                if (reply != null && reply.Error == 0)
                {
                    var rows = ConvertProfileRows(reply.Value);
                    if (rows.Count > 0)
                    {
                        Console.WriteLine($"[ReadProfileAllEntriesAsync] Range access succeeded for {obisCode}: {rows.Count} rows.");
                        return rows;
                    }
                }
                else if (reply != null && reply.Error != 0)
                {
                    Console.WriteLine($"[INFO] Range access for {obisCode} returned error {reply.Error}. Falling back to entry access...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] Selective access by range failed for {obisCode}: {ex.Message}. Falling back to entry access...");
            }

            // --- Attempt 2: ReadRowsByEntry (full buffer by entry index) ---
            uint entryCount = profile.EntriesInUse;
            Console.WriteLine($"[INFO] Reading {obisCode} by entry index (1 to {entryCount})...");

            try
            {
                var entryRequests = _client.ReadRowsByEntry(profile, 1, entryCount);
                GXReplyData? entryReply = null;

                foreach (var request in entryRequests)
                    entryReply = await SendAndReceiveAsync(request);

                if (entryReply != null && entryReply.Error == 0)
                {
                    var rows = ConvertProfileRows(entryReply.Value);
                    if (rows.Count > 0)
                    {
                        Console.WriteLine($"[ReadProfileAllEntriesAsync] Entry access succeeded for {obisCode}: {rows.Count} rows.");
                        return rows;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INFO] ReadRowsByEntry failed for {obisCode}: {ex.Message}. Falling back to raw attribute-2 buffer read...");
            }

            // --- Attempt 3: Raw attribute-2 buffer read (last resort) ---
            Console.WriteLine($"[INFO] Fallback: reading raw attribute-2 buffer for {obisCode}...");
            var value = await ReadObjectAsync(profile, 2);
            return ConvertProfileRows(value);
        }

        /// <summary>Groups a flat list of ProfileRows into a dictionary keyed by date (midnight UTC).
        /// Rows without a valid Timestamp are excluded.</summary>
        public IReadOnlyDictionary<DateTime, List<ProfileRow>> GroupRowsByDay(IReadOnlyList<ProfileRow> rows)
        {
            return rows
                .Where(r => r.Timestamp.HasValue)
                .GroupBy(r => r.Timestamp!.Value.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // =========================================================
        // CONVERT PROFILE ROWS
        //
        // Converts the raw DLMS reply value (an IEnumerable of row enumerables)
        // into typed ProfileRow objects. Each row's Timestamp is extracted from
        // the first DateTime/GXDateTime cell found in that row's values.
        //
        // FIX (preserved from prototype): each row must use its OWN freshly-extracted
        // values array — not a class-level field — otherwise all rows share the same
        // Values reference and all but the last are empty.
        // =========================================================

        private static IReadOnlyList<ProfileRow> ConvertProfileRows(object? value)
        {
            var rows = new List<ProfileRow>();

            if (value is not System.Collections.IEnumerable enumerable)
                return rows;

            int rowIndex = 0;
            foreach (var item in enumerable)
            {
                if (item is not System.Collections.IEnumerable rowEnumerable)
                    continue;

                var values = rowEnumerable.Cast<object?>().ToList();

                var row = new ProfileRow
                {
                    Timestamp = ExtractTimestamp(values.ToArray()),
                    Values = values
                };

                var contentStr = string.Join(", ", values.Select(v => v?.ToString() ?? "null"));
                Console.WriteLine($"[DEBUG ROW {rowIndex++}] Timestamp: {row.Timestamp:yyyy-MM-dd HH:mm:ss} | Content: {contentStr}");

                rows.Add(row);
            }

            return rows;
        }

        // =========================================================
        // SEND AND RECEIVE
        //
        // Two-level implementation:
        //   outer overload: handles multi-block (IsMoreData) loop
        //   inner overload: sends one request, receives one frame, calls GetData
        //
        // CRITICAL: AllData = false in BOTH ReceiveParameters instances below.
        // See inline comments for why. Do NOT change either back to true.
        // =========================================================

        private async System.Threading.Tasks.Task<GXReplyData> SendAndReceiveAsync(byte[] request)
        {
            _lastRequestBytesReceived = 0;

            var reply = new GXReplyData();
            var notify = new GXReplyData();

            await SendAndReceiveAsync(request, reply, notify);

            // Multi-block loop: if the meter signals more data is coming (IsMoreData),
            // send a ReceiverReady acknowledgement and receive the next block.
            while (reply.IsMoreData)
            {
                var nextRequest = _client.ReceiverReady(reply);
                await SendAndReceiveAsync(nextRequest, reply, notify);
            }

            return reply;
        }

        private async SysTask SendAndReceiveAsync(byte[] request, GXReplyData reply, GXReplyData notify)
        {
            var buffer = new GXByteBuffer();

            // CRITICAL: AllData = false.
            // The meter's reply arrives as a complete frame in a single receive. With
            // AllData = true, GXNet.Receive() would wait for all bytes to arrive as
            // one contiguous read — but the frame is already complete after the first
            // Receive() call. AllData = true therefore blocks for the full WaitTime
            // even though a valid frame has already arrived, causing a false
            // TimeoutException every time. Do not change this back to true.
            var receiveParameters = new ReceiveParameters<byte[]>
            {
                WaitTime = _media.WaitTime,
                AllData = false
            };

            if (_client.InterfaceType == InterfaceType.HDLC)
            {
                receiveParameters.Eop = (byte)0x7E;
            }
            else if (_client.InterfaceType == InterfaceType.WRAPPER)
            {
                // For WRAPPER, seed with 8 bytes (the fixed header size) so Receive()
                // knows when a minimal frame header has arrived.
                receiveParameters.Count = 8;
            }

            bool received;
            lock (_media.Synchronous)
            {
                _media.Send(request, null);

                if (_verboseLogging)
                    Console.WriteLine($"[SEND] ({request.Length} bytes): {BitConverter.ToString(request)}");

                received = _media.Receive(receiveParameters);
            }

            if (receiveParameters.Reply != null)
                _lastRequestBytesReceived += receiveParameters.Reply.Length;

            if (_verboseLogging)
                Console.WriteLine($"[RECV] Received: {received}, Length: {receiveParameters.Reply?.Length ?? 0}, Bytes: {(receiveParameters.Reply != null ? BitConverter.ToString(receiveParameters.Reply) : "null")}");

            if (!received)
                throw new TimeoutException("Failed to receive reply from meter.");

            if (receiveParameters.Reply == null)
                throw new InvalidOperationException("Meter returned an empty response.");

            buffer.Set(receiveParameters.Reply);

            // WRAPPER-specific: the first Receive() only fetches the 8-byte header.
            // We parse the header to find the payload size, then issue a second
            // Receive() to collect exactly the remaining bytes.
            if (_client.InterfaceType == InterfaceType.WRAPPER)
            {
                int payloadSize = _client.GetFrameSize(buffer);
                int totalSize = 8 + payloadSize;

                if (_verboseLogging)
                    Console.WriteLine($"[FRAME SIZE INFO] Header size: {buffer.Size}, PayloadSize: {payloadSize}, TotalSize: {totalSize}");

                if (buffer.Size < totalSize)
                {
                    // CRITICAL: AllData = false here too (same reason as above).
                    // We know exactly how many bytes remain (Count = totalSize - buffer.Size),
                    // so AllData = true would be redundant AND cause the same blocking problem.
                    var remainingParams = new ReceiveParameters<byte[]>
                    {
                        WaitTime = _media.WaitTime,
                        Count = totalSize - buffer.Size,
                        AllData = false
                    };

                    lock (_media.Synchronous)
                    {
                        received = _media.Receive(remainingParams);
                    }

                    if (remainingParams.Reply != null)
                        _lastRequestBytesReceived += remainingParams.Reply.Length;

                    if (_verboseLogging)
                        Console.WriteLine($"[RECV REMAINING] Success: {received}, Length: {remainingParams.Reply?.Length ?? 0}");

                    if (received && remainingParams.Reply != null)
                        buffer.Set(remainingParams.Reply);
                }
            }

            if (_verboseLogging)
                Console.WriteLine($"[BEFORE GETDATA] Buffer Size: {buffer.Size}");

            // GetData loop: parses the buffer and accumulates blocks. If not complete,
            // sends a ReceiverReady and reads the next block until GetData returns true.
            while (true)
            {
                var complete = _client.GetData(buffer, reply, notify);

                if (_verboseLogging)
                    Console.WriteLine($"[GETDATA RESULT] Complete: {complete}, Reply Error: {reply.Error}");

                if (complete)
                    break;

                var receiverReady = _client.ReceiverReady(reply);

                var nextParams = new ReceiveParameters<byte[]>
                {
                    WaitTime = _media.WaitTime
                };

                if (_client.InterfaceType == InterfaceType.HDLC)
                    nextParams.Eop = (byte)0x7E;
                else if (_client.InterfaceType == InterfaceType.WRAPPER)
                    nextParams.Count = 8;

                lock (_media.Synchronous)
                {
                    _media.Send(receiverReady, null);
                    received = _media.Receive(nextParams);
                }

                if (nextParams.Reply != null)
                    _lastRequestBytesReceived += nextParams.Reply.Length;

                if (!received)
                    throw new TimeoutException("Failed to receive next DLMS block.");

                if (nextParams.Reply == null)
                    throw new InvalidOperationException("Meter returned an empty block.");

                buffer.Clear();
                buffer.Set(nextParams.Reply);

                if (_client.InterfaceType == InterfaceType.WRAPPER)
                {
                    int payloadSize = _client.GetFrameSize(buffer);
                    int totalSize = 8 + payloadSize;

                    if (buffer.Size < totalSize)
                    {
                        var nextRemainingParams = new ReceiveParameters<byte[]>
                        {
                            WaitTime = _media.WaitTime,
                            Count = totalSize - buffer.Size
                        };

                        lock (_media.Synchronous)
                        {
                            received = _media.Receive(nextRemainingParams);
                        }

                        if (nextRemainingParams.Reply != null)
                            _lastRequestBytesReceived += nextRemainingParams.Reply.Length;

                        if (received && nextRemainingParams.Reply != null)
                            buffer.Set(nextRemainingParams.Reply);
                    }
                }
            }
        }

        // =========================================================
        // EXTRACT TIMESTAMP
        //
        // Scans the row's values array for the first DateTime-like value.
        // Applies the Year <= 1 guard: meters occasionally return GXDateTime
        // with Year=0 or Year=1 for wildcarded/invalid clock entries.
        // Returning null instead of a garbage DateTime prevents corrupt
        // watermarks and display values in Stage 4.
        // =========================================================

        private static DateTime? ExtractTimestamp(object?[] values)
        {
            foreach (var value in values)
            {
                if (value is DateTime dateTime)
                {
                    if (dateTime.Year <= 1)
                        return null; // Invalid/wildcarded clock entry — guard
                    return dateTime;
                }

                if (value is DateTimeOffset dateTimeOffset)
                {
                    if (dateTimeOffset.Year <= 1)
                        return null;
                    return dateTimeOffset.DateTime;
                }

                if (value is GXDateTime gxDateTime)
                {
                    var dt = gxDateTime.Value.DateTime;
                    if (dt.Year <= 1)
                        return null; // Invalid/wildcarded clock entry — guard
                    return dt;
                }
            }

            return null;
        }

        // =========================================================
        // CONNECTION CHECK
        // =========================================================

        private void EnsureConnected()
        {
            if (!_connected)
                throw new InvalidOperationException("Meter is not connected. Call ConnectAsync() first.");
        }

        // =========================================================
        // DISCONNECT
        // =========================================================

        /// <summary>
        /// Async disconnect: sends RLRQ and waits for the RLRE response before closing
        /// the TCP socket. This ensures the meter has fully released the session before
        /// a new connection can be established — critical for back-to-back sync runs.
        /// Called by DisposeAsync (used by ProfileSyncService's 'await using' pattern).
        /// </summary>
        public async System.Threading.Tasks.Task DisconnectAsync()
        {
            if (!_connected)
                return;

            try
            {
                // _isAssociated guard: only send ReleaseRequest if association
                // was successfully established.
                if (_isAssociated)
                {
                    var requests = _client.ReleaseRequest();
                    Console.WriteLine($"[RLRQ] ReleaseRequest() produced {requests?.Length ?? 0} packet(s).");
                    if (requests != null)
                    {
                        foreach (var request in requests)
                        {
                            // Use SendAndReceiveAsync to wait for RLRE — ensures the
                            // meter has fully torn down the session before socket closes.
                            try
                            {
                                await SendAndReceiveAsync(request);
                                Console.WriteLine($"[RLRQ] RLRE received successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[RLRQ] RLRE error (ignored): {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore disconnect errors.
            }
            finally
            {
                _isAssociated = false;
            }

            _media.Close();
            _connected = false;

            // Record disconnect time for cooldown enforcement on next connect.
            string key = $"{_device.IP}:{_device.PORT}";
            _meterLastDisconnect[key] = DateTime.UtcNow;
            Console.WriteLine($"[COOLDOWN] Recorded disconnect for {key} at {_meterLastDisconnect[key]:HH:mm:ss} UTC.");
        }

        /// <summary>
        /// Synchronous disconnect: fires RLRQ best-effort without waiting for RLRE.
        /// Used by the synchronous Dispose() path only. Prefer 'await using' (DisposeAsync)
        /// so that DisconnectAsync runs and the meter's session is properly released.
        /// </summary>
        public void Disconnect()
        {
            if (!_connected)
                return;

            try
            {
                if (_isAssociated)
                {
                    var requests = _client.ReleaseRequest();
                    Console.WriteLine($"[RLRQ-SYNC] ReleaseRequest() produced {requests?.Length ?? 0} packet(s) (sync path, best-effort).");
                    if (requests != null)
                    {
                        foreach (var request in requests)
                        {
                            // Best-effort fire-and-forget — sync path only.
                            try { _media.Send(request, null); } catch { }
                        }
                    }
                }
            }
            catch
            {
                // Ignore disconnect errors.
            }
            finally
            {
                _isAssociated = false;
            }

            _media.Close();
            _connected = false;

            // Record disconnect time for cooldown enforcement on next connect.
            string key = $"{_device.IP}:{_device.PORT}";
            _meterLastDisconnect[key] = DateTime.UtcNow;
            Console.WriteLine($"[COOLDOWN-SYNC] Recorded disconnect for {key} at {_meterLastDisconnect[key]:HH:mm:ss} UTC.");
        }

        // =========================================================
        // DISPOSE / ASYNC DISPOSE
        // =========================================================

        public void Dispose()
        {
            Disconnect();
            _media.Dispose();
        }

        /// <summary>
        /// Async dispose: awaits DisconnectAsync so the RLRE is received before closing.
        /// Always prefer 'await using var reader = new DlmsMeterReader(device)' over
        /// 'using var reader = ...' to ensure proper session teardown.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            _media.Dispose();
        }
    }
}
