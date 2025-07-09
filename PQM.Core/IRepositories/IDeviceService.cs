using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IDeviceService
    {
        int AddDevice(Device device);
        IQueryable<Device> GetDevices();
    }
}
