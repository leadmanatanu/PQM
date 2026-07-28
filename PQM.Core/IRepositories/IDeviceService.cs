using PQM.Core.Entities;
using System;
using System.Linq;

namespace PQM.Core.IRepositories
{
    public interface IDeviceService
    {
        int AddDevice(Device device);
        bool UpdateDevice(Device device);
        IQueryable<Device> GetDevices();
        bool UpdateLastSync(int id, DateTime syncDate);
        bool DeleteDevice(int id);
    }
}
