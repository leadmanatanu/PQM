using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;

namespace PQM.Infrastructure.Repositories
{
    public class SyncRunLogRepository : ISyncRunLogRepository
    {
        private readonly DataContext _context;

        public SyncRunLogRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<SyncRunLogs> CreateScheduleRunAsync(SyncRunLogs runLog)
        {
            _context.SyncRunLogs.Add(runLog);

            await _context.SaveChangesAsync();

            return runLog;
        }

        public async Task<SyncDeviceRunLogs> CreateDeviceRunAsync(
            SyncDeviceRunLogs deviceRunLog)
        {
            _context.SyncDeviceRunLogs.Add(deviceRunLog);

            await _context.SaveChangesAsync();

            return deviceRunLog;
        }

        public async Task UpdateDeviceRunAsync(
            SyncDeviceRunLogs deviceRunLog)
        {
            _context.SyncDeviceRunLogs.Update(deviceRunLog);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateScheduleRunAsync(
            SyncRunLogs runLog)
        {
            _context.SyncRunLogs.Update(runLog);

            await _context.SaveChangesAsync();
        }
    }
}