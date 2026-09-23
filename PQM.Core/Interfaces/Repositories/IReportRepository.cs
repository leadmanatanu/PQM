using PQM.Core.DTOs;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IReportRepository
    {
        List<ParameterValueSearch> GetAggregatedReport(ReportSearch searchParams, int intervalMinutes);
    }
}