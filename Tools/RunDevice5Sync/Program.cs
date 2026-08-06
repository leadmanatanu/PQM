using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure.Services;

const string ConnStr = "Server=Blade;Database=PQM;Integrated Security=True;TrustServerCertificate=True;";

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<ProfileSyncService>();
var syncService = new ProfileSyncService(ConnStr, logger);

Console.WriteLine("=================================================");
Console.WriteLine("STARTING REAL PRODUCTION SYNC SWEEP FOR DEVICE 5");
Console.WriteLine("=================================================");

var sw = System.Diagnostics.Stopwatch.StartNew();
var result = await syncService.SyncDeviceAllProfilesAsync(5);
sw.Stop();

Console.WriteLine();
Console.WriteLine("=================================================");
Console.WriteLine($"SYNC SWEEP COMPLETED in {sw.Elapsed.TotalMinutes:F2} minutes ({sw.Elapsed.TotalSeconds:F0} seconds)");
Console.WriteLine($"Success          : {result.Success}");
Console.WriteLine($"ProfilesSucceeded: {result.ProfilesSucceeded} / {result.ProfilesAttempted}");
Console.WriteLine($"TotalRowsRead    : {result.TotalRowsRead}");
Console.WriteLine($"TotalRowsWritten : {result.TotalRowsWritten}");
Console.WriteLine($"TotalRowsSkipped : {result.TotalRowsSkipped}");
if (!string.IsNullOrEmpty(result.ErrorMessage))
{
    Console.WriteLine($"Error            : {result.ErrorMessage}");
}
Console.WriteLine("=================================================");
Console.WriteLine();

foreach (var (obis, res) in result.ProfileResults)
{
    var status = res.Success ? "OK" : $"FAILED ({res.ErrorMessage})";
    Console.WriteLine($"  [{obis,-20}] -> {status,-30} Read: {res.RowsRead,5} | Written: {res.RowsWritten,5} | Skipped: {res.RowsSkipped,5} | Watermark: {res.NewWatermarkUtc:yyyy-MM-dd HH:mm:ss UTC}");
}

return result.Success ? 0 : 1;
