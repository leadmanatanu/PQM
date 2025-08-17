using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IDeviceLogService
    {
        bool AddDeviceLogs(List<DeviceLog> deviceLogs);
        IQueryable<DeviceLog> GetDeviceLogs();
        bool AddBulkDeviceLogs(List<DeviceLog> deviceLogs);
        (List<DeviceLogSearch>, int) GetDeviceLogs(int deviceId, int parameterId, int pageNumber, int pageSize, DateTime startDate, DateTime endDate);
        bool AddDeviceEventLogs(List<EventLog> eventLogs);
    }
}
