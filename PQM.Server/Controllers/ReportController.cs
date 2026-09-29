using Microsoft.AspNetCore.Mvc;
using PQM.Core.DTOs;
using PQM.Core.Interfaces.Repositories;
using PQM.Server.Models;
using System.Text;
using ClosedXML.Excel;

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

        var device = await _reportRepository.GetDeviceByIdAsync(
    searchParams.DeviceId, cancellationToken);

        var profiles = await _reportRepository.GetProfilesByDeviceIdAsync(
            searchParams.DeviceId, cancellationToken);

        var profileLookup = profiles.ToDictionary(
            p => p.Id,
            p => string.IsNullOrWhiteSpace(p.FriendlyName)
                ? p.ObisCode ?? $"Profile {p.Id}"
                : p.FriendlyName);

        var data = _reportRepository.GetAggregatedReportForExport(
            searchParams, searchParams.IntervalMinutes);

        using var workbook = new XLWorkbook();
        if (data == null || !data.Any())
        {
            return BadRequest("No report data found for the selected date range and profiles.");
        }

        foreach (var profileGroup in data.GroupBy(x => x.ProfileId))
        {
            var profileName = profileLookup.TryGetValue(
    profileGroup.Key ?? 0, out var name)
        ? name
        : $"Profile_{profileGroup.Key}";
            var sheetName = profileName.Length > 31
                ? profileName[..31]
                : profileName;

            var ws = workbook.Worksheets.Add(sheetName);

            var parameters = profileGroup
                .GroupBy(x => x.ParameterId)
                .Select(x => new
                {
                    Id = x.Key,
                    Name = x.First().ParameterName
                })
                .ToList();

            ws.Cell(1, 1).Value = "Timestamp";

            for (int i = 0; i < parameters.Count; i++)
                ws.Cell(1, i + 2).Value = parameters[i].Name;

            var timestamps = profileGroup
                .Select(x => x.DateStamp)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            for (int r = 0; r < timestamps.Count; r++)
            {
                ws.Cell(r + 2, 1).Value = timestamps[r];

                for (int c = 0; c < parameters.Count; c++)
                {
                    var value = profileGroup.FirstOrDefault(x =>
                        x.ParameterId == parameters[c].Id &&
                        x.DateStamp == timestamps[r]);

                    if (value != null)
                        ws.Cell(r + 2, c + 2).Value = value.Value;
                }
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Column(1).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
            ws.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName =
            $"{device.Name}_{device.SerialNumber}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
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