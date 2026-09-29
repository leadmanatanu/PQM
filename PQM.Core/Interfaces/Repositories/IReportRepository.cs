using PQM.Core.DTOs;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task<List<ProfileDropdownDto>> GetProfilesByDeviceIdAsync(
            int deviceId,
            CancellationToken cancellationToken);

        Task<List<ParameterDropdownDto>> GetParametersByProfileIdAsync(
            int profileId,
            CancellationToken cancellationToken);
        AggregatedReportResult GetAggregatedReport(
            ReportSearch searchParams,
            int intervalMinutes);

        List<ParameterValueSearch> GetAggregatedReportForExport(
            ReportSearch searchParams,
            int intervalMinutes);
    }
}