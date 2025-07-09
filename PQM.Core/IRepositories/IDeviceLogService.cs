using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IDeviceLogService
    {
        bool AddDeviceLogs(List<DeviceLog> deviceLogs);
        IQueryable<DeviceLog> GetDeviceLogs();
        bool AddBulkDeviceLogs(List<DeviceLog> deviceLogs);
    }
}
