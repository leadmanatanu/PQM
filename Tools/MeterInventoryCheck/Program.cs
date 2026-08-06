// =============================================================================
// MeterInventoryCheck — Standalone DLMS Meter Diagnostic Tool
// =============================================================================
//
// PURPOSE:
//   Connects directly to a physical DLMS meter (device 5 by default) and
//   reports exactly what data currently exists on it — profile by profile —
//   as ground truth for manual comparison against the database.
//
// THIS TOOL:
//   - Is completely READ-ONLY. It writes NOTHING to the database.
//   - Reuses DlmsMeterReader from PQM.Infrastructure exactly as the real app.
//   - Fetches device connection settings live from the Devices table (raw ADO.NET)
//     so it always uses the same IP/port/auth/password the real app uses.
//   - For time-series profiles: reads attr 3 (schema) + attr 7 (EntriesInUse count)
//     + first entry + last entry ONLY — does NOT pull the full buffer.
//   - For static/metadata profiles: reads attribute 2 and shows a brief sample.
//   - Uses DlmsMeterReader.ReadProfileInventoryAsync() — a dedicated minimal-read
//     method added to the existing class specifically for this diagnostic.
//
// CONCURRENCY WARNING (printed at startup):
//   This tool opens its own independent DLMS association on the meter.
//   Most DLMS meters support only ONE application-layer association at a time.
//   DO NOT run while PQM.Console is actively mid-sync for this device.
//
// USAGE:
//   cd D:\PQM\Tools\MeterInventoryCheck
//   dotnet run
//   dotnet run -- --device-id 5          (explicit device id, default = 5)
//   dotnet run -- --verbose              (enable Gurux frame tracing)
//   dotnet run -- --cooldown 0           (skip inter-session cooldown for this standalone run)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gurux.DLMS.Objects;
using Microsoft.Data.SqlClient;
using PQM.Core.Entities;
using PQM.Infrastructure.Services;

// ─── Parse args ───────────────────────────────────────────────────────────────
int deviceId = 5;
bool verbose = false;
int cooldown = 8; // seconds — same default as PQM.Console appsettings.json
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--device-id" && i + 1 < args.Length) deviceId = int.Parse(args[++i]);
    if (args[i] == "--verbose") verbose = true;
    if (args[i] == "--cooldown" && i + 1 < args.Length) cooldown = int.Parse(args[++i]);
}

// ─── Connection string (mirrors PQM.Console/appsettings.json) ─────────────────
const string ConnStr =
    "Server=Blade;Database=PQM;Integrated Security=True;TrustServerCertificate=True;";

// ─── Console setup ────────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     PQM METER INVENTORY CHECK  —  READ-ONLY LIVE DIAGNOSTIC     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine($"  Run time (local): {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
Console.WriteLine($"  Target device ID: {deviceId}");
Console.WriteLine($"  Verbose DLMS log: {(verbose ? "YES" : "no")}");
Console.WriteLine($"  Cooldown seconds: {cooldown}");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("  ⚠  CONCURRENCY WARNING");
Console.WriteLine("     This tool opens an independent DLMS association on the meter.");
Console.WriteLine("     DLMS meters typically support only ONE association at a time.");
Console.WriteLine("     Ensure PQM.Console is NOT mid-sync for this device before running.");
Console.ResetColor();
Console.WriteLine();

// ─── Step 1: Fetch device from DB ────────────────────────────────────────────
Console.Write($"  [1/4] Loading device {deviceId} from Devices table ... ");
Device device;
try
{
    device = await FetchDeviceAsync(deviceId, ConnStr);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("OK");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"FAILED\n  {ex.Message}");
    Console.ResetColor();
    return 1;
}

Console.WriteLine($"  Name    : {device.Name}");
Console.WriteLine($"  IP:Port : {device.IP}:{device.PORT}");
Console.WriteLine($"  Auth    : {device.Authentication} (TypeId={device.AuthenticationTypeId})");
Console.WriteLine($"  Client  : {device.ClientAddress ?? 32}   Server: {device.ServerAddress ?? 1}");
Console.WriteLine($"  Password: {(string.IsNullOrEmpty(device.Password) ? "(none)" : $"[{device.Password!.Length} chars — hidden]")}");
Console.WriteLine($"  Timeout : {device.Timeout ?? 30000} ms");
Console.WriteLine($"  TimeZone: {device.TimeZoneId ?? "(not set)"}");
Console.WriteLine();

// ─── Step 2: Connect + read association view ──────────────────────────────────
Console.Write("  [2/4] Connecting to meter and reading association view ... ");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n  Cancellation requested — cleaning up...");
    cts.Cancel();
};

var reader = new DlmsMeterReader(device, verboseLogging: verbose, meterCooldownSeconds: cooldown);
int profileGenericsInAV = 0;
try
{
    await reader.ConnectAsync(cts.Token);
    var avProfiles = await reader.ReadAssociationViewAsync(cts.Token);
    profileGenericsInAV = avProfiles.Count;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"OK ({profileGenericsInAV} ProfileGeneric objects in association view)");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"FAILED\n  {ex.Message}");
    Console.ResetColor();
    await reader.DisposeAsync();
    return 2;
}
Console.WriteLine();

// ─── Step 3: Query each profile ──────────────────────────────────────────────
Console.WriteLine("  [3/4] Querying all 17 catalog profiles ...");
Console.WriteLine();

var results = new List<ProfileResult>();
int profileNum = 0;
int total = ProfileCatalog.AllProfiles.Count;

// Time-series profiles — use lightweight inventory read (count + boundary timestamps)
foreach (var (obis, name) in ProfileCatalog.TimeSeriesProfiles)
{
    cts.Token.ThrowIfCancellationRequested();
    profileNum++;
    Console.Write($"    [{profileNum:D2}/{total:D2}] {name,-30} ({obis}) ... ");

    var r = new ProfileResult { Obis = obis, Name = name, IsTimeSeries = true };
    try
    {
        var inv = await reader.ReadProfileInventoryAsync(obis, cts.Token);
        r.EntriesInUse = inv.EntriesInUse;
        r.Earliest = inv.Earliest;
        r.Latest = inv.Latest;
        r.Status = "OK";

        // Collect per-field warnings for display
        var warnings = new List<string>();
        if (inv.EntriesInUseError != null) warnings.Add($"count-err: {Trunc(inv.EntriesInUseError, 40)}");
        if (inv.EarliestError  != null) warnings.Add($"first-err: {Trunc(inv.EarliestError, 40)}");
        if (inv.LatestError    != null) warnings.Add($"last-err: {Trunc(inv.LatestError, 40)}");
        r.Warnings = warnings;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"OK ");
        Console.ResetColor();
        Console.WriteLine($"  count={r.EntriesInUse?.ToString() ?? "?"} earliest={r.Earliest:yyyy-MM-dd HH:mm} latest={r.Latest:yyyy-MM-dd HH:mm}");
    }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex)
    {
        r.Status = "ERROR";
        r.ErrorMessage = ex.ToString();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR  {Trunc(ex.Message, 70)}");
        Console.ResetColor();
    }
    results.Add(r);
}

// Static/metadata profiles — read attribute 2 for a sample
foreach (var (obis, name) in ProfileCatalog.StaticOrMetadataProfiles)
{
    cts.Token.ThrowIfCancellationRequested();
    profileNum++;
    Console.Write($"    [{profileNum:D2}/{total:D2}] {name,-30} ({obis}) ... ");

    var r = new ProfileResult { Obis = obis, Name = name, IsTimeSeries = false };
    try
    {
        var obj = reader.FindObjectByObis(obis);
        if (obj is not GXDLMSProfileGeneric profile)
        {
            r.Status = "NOT FOUND";
            r.ErrorMessage = $"Object {obis} not in association view (not even after fallback insertion).";
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("NOT FOUND");
            Console.ResetColor();
            results.Add(r);
            continue;
        }

        var value = await reader.ReadObjectAsync(profile, 2, cts.Token);
        r.StaticSample = ExtractSample(value);
        r.Status = "OK";
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("OK");
        Console.ResetColor();
    }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex)
    {
        r.Status = "ERROR";
        r.ErrorMessage = ex.ToString();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR  {Trunc(ex.Message, 70)}");
        Console.ResetColor();
    }
    results.Add(r);
}

// ─── Step 4: Disconnect ───────────────────────────────────────────────────────
Console.WriteLine();
Console.Write("  [4/4] Disconnecting from meter cleanly ... ");
await reader.DisposeAsync();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();
Console.WriteLine();

// ─── Summary table ────────────────────────────────────────────────────────────
PrintSummaryTable(results);
return 0;

// =============================================================================
// HELPERS
// =============================================================================

static void PrintSummaryTable(List<ProfileResult> results)
{
    const int c1 = 33, c2 = 20, c3 = 8, c4 = 9, c5 = 20, c6 = 20;

    string H(string s, int w) => s.PadRight(w);
    string D(int w) => new string('-', w);

    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              METER INVENTORY — GROUND TRUTH  (live read from physical meter, READ-ONLY, no DB writes)               ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"  {H("Profile Name", c1)} | {H("OBIS Code", c2)} | {H("Status", c3)} | {H("Entries", c4)} | {H("Earliest (local)", c5)} | {H("Latest (local)", c6)}");
    Console.WriteLine($"  {D(c1)}-+-{D(c2)}-+-{D(c3)}-+-{D(c4)}-+-{D(c5)}-+-{D(c6)}");

    string tsGroup = "";
    foreach (var r in results)
    {
        // Print section separator when switching between time-series and static
        string newGroup = r.IsTimeSeries ? "TIME-SERIES" : "STATIC/METADATA";
        if (newGroup != tsGroup)
        {
            tsGroup = newGroup;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ── {tsGroup} ──");
            Console.ResetColor();
        }

        string name     = Trunc(r.Name, c1) ?? "";
        string obis     = r.Obis;
        string entries  = r.IsTimeSeries ? (r.EntriesInUse?.ToString() ?? "?") : "(static)";
        string earliest = r.IsTimeSeries ? (r.Earliest?.ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a") : "n/a";
        string latest   = r.IsTimeSeries ? (r.Latest?.ToString("yyyy-MM-dd HH:mm:ss")   ?? "n/a") : "n/a";

        Console.Write($"  {name.PadRight(c1)} | {obis.PadRight(c2)} | ");

        if (r.Status == "OK")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("OK      ");
        }
        else if (r.Status == "NOT FOUND")
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("NOTFOUND");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR   ");
        }
        Console.ResetColor();

        Console.Write($" | {entries.PadRight(c4)} | {earliest.PadRight(c5)} | {latest.PadRight(c6)}");

        // Append any per-field warnings inline
        if (r.Warnings?.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("  ⚠ " + string.Join("; ", r.Warnings));
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  Run completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
    Console.ResetColor();
    Console.WriteLine();

    // ── Error details ─────────────────────────────────────────────────────────
    var failed = results.Where(r => r.Status != "OK").ToList();
    if (failed.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ── ERROR DETAILS ({failed.Count} profile(s) ─────────────────────────────────────────────────");
        Console.ResetColor();
        foreach (var r in failed)
        {
            Console.WriteLine($"  [{r.Obis}] {r.Name}:");
            Console.ForegroundColor = ConsoleColor.Red;
            // Print first 400 chars of the exception so we see the type + message without flooding
            var msg = r.ErrorMessage ?? "(no message)";
            Console.WriteLine("    " + (msg.Length > 400 ? msg[..400] + "\n    [truncated]" : msg));
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    // ── Static profile samples (sanity check) ────────────────────────────────
    var statics = results.Where(r => !r.IsTimeSeries && r.Status == "OK").ToList();
    if (statics.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ── STATIC / METADATA PROFILE SAMPLES (sanity check that connection is real) ─────────────────");
        Console.ResetColor();
        foreach (var r in statics)
        {
            Console.WriteLine($"  [{r.Obis}] {r.Name}:");
            Console.WriteLine($"    {r.StaticSample ?? "(no sample)"}");
        }
        Console.WriteLine();
    }
}

static string ExtractSample(object? value)
{
    if (value == null) return "(null response)";

    if (value is System.Collections.IEnumerable en && value is not string)
    {
        var rows = en.Cast<object?>().Take(3).ToList();
        if (rows.Count == 0) return "(empty buffer)";

        return string.Join("\n    ", rows.Select(row =>
        {
            if (row is System.Collections.IEnumerable rowEn && row is not string)
                return "[" + string.Join(", ", rowEn.Cast<object?>().Take(10).Select(v => Trunc(v?.ToString(), 25))) + "]";
            return Trunc(row?.ToString(), 80) ?? "null";
        }));
    }
    return Trunc(value.ToString(), 120) ?? "(null)";
}

static string? Trunc(string? s, int max) =>
    s == null ? null : (s.Length <= max ? s : s[..max] + "…");

static async Task<Device> FetchDeviceAsync(int id, string connStr)
{
    await using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT Id, Name, IP, PORT, ClientAddress, ServerAddress,
               AuthenticationTypeId, Password, Timeout, TimeZoneId,
               IsActive, IsDeleted, CreatedDate
        FROM   Devices
        WHERE  Id = @id AND IsDeleted = 0";
    cmd.Parameters.AddWithValue("@id", id);

    await using var rdr = await cmd.ExecuteReaderAsync();
    if (!await rdr.ReadAsync())
        throw new InvalidOperationException(
            $"Device Id={id} not found (or IsDeleted=1) in the Devices table.");

    int oId       = rdr.GetOrdinal("Id");
    int oName     = rdr.GetOrdinal("Name");
    int oIp       = rdr.GetOrdinal("IP");
    int oPort     = rdr.GetOrdinal("PORT");
    int oClient   = rdr.GetOrdinal("ClientAddress");
    int oServer   = rdr.GetOrdinal("ServerAddress");
    int oAuth     = rdr.GetOrdinal("AuthenticationTypeId");
    int oPw       = rdr.GetOrdinal("Password");
    int oTimeout  = rdr.GetOrdinal("Timeout");
    int oTz       = rdr.GetOrdinal("TimeZoneId");
    int oActive   = rdr.GetOrdinal("IsActive");
    int oDeleted  = rdr.GetOrdinal("IsDeleted");
    int oCreated  = rdr.GetOrdinal("CreatedDate");

    return new Device
    {
        Id                   = rdr.GetInt32(oId),
        Name                 = rdr.GetString(oName),
        IP                   = rdr.GetString(oIp),
        PORT                 = rdr.GetInt32(oPort),
        ClientAddress        = rdr.IsDBNull(oClient)  ? null : rdr.GetInt32(oClient),
        ServerAddress        = rdr.IsDBNull(oServer)  ? null : rdr.GetInt32(oServer),
        AuthenticationTypeId = rdr.IsDBNull(oAuth)    ? null : rdr.GetInt32(oAuth),
        Password             = rdr.IsDBNull(oPw)      ? null : rdr.GetString(oPw),
        Timeout              = rdr.IsDBNull(oTimeout)  ? 30000 : rdr.GetInt32(oTimeout),
        TimeZoneId           = rdr.IsDBNull(oTz)      ? null : rdr.GetString(oTz),
        IsActive             = rdr.GetBoolean(oActive),
        IsDeleted            = rdr.GetBoolean(oDeleted),
        CreatedDate          = rdr.GetDateTime(oCreated),
    };
}

// =============================================================================
// RESULT MODEL
// =============================================================================
sealed class ProfileResult
{
    public string Obis { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsTimeSeries { get; set; }
    public string Status { get; set; } = "PENDING";
    public int? EntriesInUse { get; set; }
    public DateTime? Earliest { get; set; }
    public DateTime? Latest { get; set; }
    public List<string>? Warnings { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StaticSample { get; set; }
}
