using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IDeviceService
    {
        int AddDevice(Device device);
        bool UpdateDevice(Device device);
        bool DeleteDevice(int id);
        IQueryable<Device> GetDevices();
        bool UpdateLastSync(int id, DateTime syncDate);
    }
}
