using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Models;
using System.Text;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly APIResponse _apiResponse = new();
    private readonly IReportRepository _reportRepository;

    public ReportController(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    private static DateTime GetIndiaStandardTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
    }

    [HttpGet("aggregate")]
    public async Task<IActionResult> GetAggregatedReport(
        [FromQuery] ReportSearch searchParams,
        CancellationToken cancellationToken)
    {
        try
        {
            if (searchParams.DeviceId <= 0)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode =
                    System.Net.HttpStatusCode.BadRequest;

                _apiResponse.Errors = new List<string>
                {
                    "DeviceId is required."
                };

                return Ok(_apiResponse);
            }

            int interval = searchParams.IntervalMinutes > 0
                ? searchParams.IntervalMinutes
                : 15;

            // ---------------------------------------------------------
            // Get profiles
            // ---------------------------------------------------------

            var profiles = await _reportRepository.GetProfilesByDeviceIdAsync(searchParams.DeviceId,cancellationToken);

            var profileLookup = profiles.ToDictionary(
                p => p.Id,
                p => string.IsNullOrWhiteSpace(p.FriendlyName)
                    ? p.ObisCode ?? $"Profile {p.Id}"
                    : p.FriendlyName
            );
            // ---------------------------------------------------------
            // Find BlockLoad Profile
            // ---------------------------------------------------------

            var blockLoadProfile = profiles.FirstOrDefault(p =>string.Equals(string.IsNullOrWhiteSpace(p.FriendlyName)
            ? p.ObisCode
            : p.FriendlyName,
                    "BlockLoad Profile",
                    StringComparison.OrdinalIgnoreCase
                )
            );

            // Tell repository which profile should be paginated.
            searchParams.BlockLoadProfileId = blockLoadProfile?.Id;

            // ---------------------------------------------------------
            // Get aggregated report
            // ---------------------------------------------------------

            var result =
                _reportRepository.GetAggregatedReport(
                    searchParams,
                    interval);

            // ---------------------------------------------------------
            // Add profile names
            // ---------------------------------------------------------

            foreach (var item in result.Items)
            {
                if (item.ProfileId.HasValue &&
                    profileLookup.TryGetValue(
                        item.ProfileId.Value,
                        out var profileName))
                {
                    item.ProfileName = profileName;
                }
            }

            // ---------------------------------------------------------
            // Group by profile
            // ---------------------------------------------------------

            var groups = result.Items
                .GroupBy(x => new
                {
                    x.ProfileId,
                    x.ProfileName
                })
                .OrderBy(g => g.Key.ProfileName)
                .Select(g => new
                {
                    profileId = g.Key.ProfileId,

                    profileName = g.Key.ProfileName,

                    items = g.ToList(),

                    // Pagination information is returned ONLY
                    // for BlockLoad Profile.
                    pagination =
                        blockLoadProfile != null &&
                        g.Key.ProfileId == blockLoadProfile.Id
                            ? result.Pagination
                            : null
                })
                .ToList();

            // ---------------------------------------------------------
            // Response
            // ---------------------------------------------------------

            _apiResponse.Status = true;
            _apiResponse.StatusCode =
                System.Net.HttpStatusCode.OK;

            _apiResponse.Data = new
            {
                groups
            };

            return Ok(_apiResponse);
        }
        catch (Exception ex)
        {
            _apiResponse.Status = false;
            _apiResponse.StatusCode =
                System.Net.HttpStatusCode.InternalServerError;

            _apiResponse.Errors = new List<string>
            {
                ex.Message
            };

            return Ok(_apiResponse);
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAggregatedReport(
    [FromQuery] ReportSearch searchParams,
    CancellationToken cancellationToken)
    {
        if (searchParams.DeviceId <= 0)
            return BadRequest("DeviceId is required.");

        Device device;
        try { device = await _reportRepository.GetDeviceByIdAsync(searchParams.DeviceId, cancellationToken); }
        catch (KeyNotFoundException) { return NotFound("Device not found."); }

        var profiles = await _reportRepository.GetProfilesByDeviceIdAsync(
            searchParams.DeviceId, cancellationToken);

        var profileLookup = profiles.ToDictionary(
            p => p.Id,
            p => string.IsNullOrWhiteSpace(p.FriendlyName)
                ? p.ObisCode ?? $"Profile {p.Id}"
                : p.FriendlyName!);

        var data = await _reportRepository.GetAggregatedReportForExport(
            searchParams,
            searchParams.IntervalMinutes,
            cancellationToken);

        if (data.Count == 0)
            return BadRequest("No report data found for the selected date range and profiles.");

        const int ExcelMaxRows = 1_048_575; // 1 header row
        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();

        foreach (var profileGroup in data.GroupBy(x => x.ProfileId))
        {
            // ---- unique parameter columns (order preserved from SQL ORDER BY) ----
            var paramIndex = new Dictionary<int, int>();
            var headers = new List<string> { "Timestamp" };
            foreach (var r in profileGroup)
            {
                if (!paramIndex.ContainsKey(r.ParameterId))
                {
                    paramIndex[r.ParameterId] = headers.Count;   // column offset (0 = timestamp)
                    headers.Add(r.ParameterName);
                }
            }

            // ---- unique timestamps, sorted ----
            var timestamps = profileGroup.Select(x => x.DateStamp).Distinct().OrderBy(x => x).ToList();
            if (timestamps.Count > ExcelMaxRows)
                return BadRequest("Too many timestamps for one Excel sheet. Increase interval or narrow the date range.");

            var tsIndex = new Dictionary<DateTime, int>(timestamps.Count);
            for (int i = 0; i < timestamps.Count; i++) tsIndex[timestamps[i]] = i;

            // ---- pivot in ONE pass: O(N) instead of O(N * T * P) ----
            var grid = new object?[timestamps.Count][];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new object?[headers.Count];
                grid[i][0] = timestamps[i];
            }
            foreach (var r in profileGroup)
                grid[tsIndex[r.DateStamp]][paramIndex[r.ParameterId]] = r.Value;

            // ---- sheet ----
            var ws = workbook.Worksheets.Add(
                MakeSheetName(profileLookup.TryGetValue(profileGroup.Key ?? 0, out var n)
                    ? n : $"Profile_{profileGroup.Key}", usedSheetNames));

            ws.Cell(1, 1).InsertData(new[] { headers.ToArray() });   // header row
            ws.Cell(2, 1).InsertData(grid);                          // bulk insert (much faster than cell-by-cell)

            ws.Row(1).Style.Font.Bold = true;
            ws.Column(1).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
            ws.Column(1).Width = 20;
            for (int c = 2; c <= headers.Count; c++) ws.Column(c).Width = 18;
            // NOTE: do NOT call ws.Columns().AdjustToContents() - it scans every cell.
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;   // no ToArray() copy

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{device.Name}_{device.SerialNumber}.xlsx");
    }

    private static string MakeSheetName(string raw, HashSet<string> used)
    {
        var clean = new string(raw.Where(ch => !@":\/?*[]".Contains(ch)).ToArray()).Trim();
        if (clean.Length == 0) clean = "Sheet";
        if (clean.Length > 31) clean = clean[..31];

        var name = clean;
        int i = 2;
        while (!used.Add(name))
        {
            var suffix = $"_{i++}";
            name = clean[..Math.Min(clean.Length, 31 - suffix.Length)] + suffix;
        }
        return name;
    }

    [HttpGet("profiles")]
    public async Task<IActionResult> GetProfilesByDevice(
    [FromQuery] int deviceId,
    CancellationToken cancellationToken)
    {
        try
        {
            if (deviceId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "DeviceId is required."
                });
            }

            var profiles =
                await _reportRepository.GetProfilesByDeviceIdAsync(
                    deviceId,
                    cancellationToken);

            return Ok(new
            {
                status = true,
                data = profiles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    status = false,
                    message = ex.Message
                });
        }
    }

    [HttpGet("parameters")]
    public async Task<IActionResult> GetParametersByProfile(
    [FromQuery] int profileId,
    CancellationToken cancellationToken)
    {
        try
        {
            if (profileId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "ProfileId is required."
                });
            }

            var parameters =
                await _reportRepository.GetParametersByProfileIdAsync(
                    profileId,
                    cancellationToken);

            return Ok(new
            {
                status = true,
                data = parameters
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    status = false,
                    message = ex.Message
                });
        }
    }
}