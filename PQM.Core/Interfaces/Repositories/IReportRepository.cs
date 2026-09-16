using PQM.Core.DTOs;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IReportRepository
    {
        (int TotalTimestamps, List<ParameterValueSearch> Results) GetAggregatedReport(ReportSearch searchParams,int intervalMinutes,int pageNumber,int pageSize);
    }
}