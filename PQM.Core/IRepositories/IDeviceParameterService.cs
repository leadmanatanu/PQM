using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IDeviceParameterService
    {
        bool AddDeviceParameterMapping(List<DeviceParameterMapping> data);
        IQueryable<DeviceParameterMapping> GetDeviceParameterMapping(int deviceId);
    }
}
