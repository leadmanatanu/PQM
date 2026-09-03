using PQM.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> AddAsync(Device device, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Device device, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> EnableSyncAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> DisableSyncAsync(int id, CancellationToken cancellationToken = default);
        Task QueueSyncRequestAsync(int deviceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DeviceSyncHistory>> GetSyncHistoryAsync(int deviceId, int take = 50, CancellationToken cancellationToken = default);
        Task<DeviceSyncSchedule?> GetScheduleAsync(CancellationToken cancellationToken = default);
        Task UpsertScheduleAsync(DeviceSyncSchedule schedule, CancellationToken cancellationToken = default);
    }
}
