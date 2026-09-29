using PQM.Core.DTOs;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IReportRepository
    {
        // Used by the report UI.
        // BlockLoad Profile timestamps are paginated.
        // Other profiles return all timestamps.
        AggregatedReportResult GetAggregatedReport(
            ReportSearch searchParams,
            int intervalMinutes);

        // Used by Export.
        // Returns ALL timestamps, including BlockLoad Profile.
        // No pagination is applied.
        List<ParameterValueSearch> GetAggregatedReportForExport(
            ReportSearch searchParams,
            int intervalMinutes);
    }
}