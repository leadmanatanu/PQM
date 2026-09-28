using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface ISyncRunLogRepository
    {
        Task<SyncRunLogs> CreateScheduleRunAsync(SyncRunLogs runLog);

        Task<SyncDeviceRunLogs> CreateDeviceRunAsync(SyncDeviceRunLogs deviceRunLog);

        Task UpdateDeviceRunAsync(SyncDeviceRunLogs deviceRunLog);

        Task UpdateScheduleRunAsync(SyncRunLogs runLog);
    }
}
