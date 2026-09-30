using PQM.Core.DTOs;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task<Device> GetDeviceByIdAsync(
            int deviceId,
            CancellationToken cancellationToken);
        Task<List<ProfileDropdownDto>> GetProfilesByDeviceIdAsync(
            int deviceId,
            CancellationToken cancellationToken);

        Task<List<ParameterDropdownDto>> GetParametersByProfileIdAsync(
            int profileId,
            CancellationToken cancellationToken);
        AggregatedReportResult GetAggregatedReport(
            ReportSearch searchParams,
            int intervalMinutes);

        //List<ParameterValueSearch> GetAggregatedReportForExport(
        //    ReportSearch searchParams,
        //    int intervalMinutes);

        Task<List<ExportReportRow>> GetAggregatedReportForExport(
            ReportSearch searchParams,
            int intervalMinutes,
            CancellationToken cancellationToken = default);
    }
}