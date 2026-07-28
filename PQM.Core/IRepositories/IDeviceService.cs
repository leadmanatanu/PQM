using PQM.Core.Entities;
using System;
using System.Linq;
using PQM.Core.DTOs;
namespace PQM.Core.IRepositories
{
    public interface IDeviceService
    {
        int AddDevice(Device device);
        bool UpdateDevice(Device device);
        IQueryable<DeviceDto> GetDevices();
        bool UpdateLastSync(int id, DateTime syncDate);
        bool DeleteDevice(int id);
    }
}
