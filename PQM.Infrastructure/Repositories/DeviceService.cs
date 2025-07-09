using PQM.Core.Entities;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceService : IDeviceService
    {
        public string _connectionString { get; set; }

        public DeviceService(string connectionString)
        {
            this._connectionString = connectionString;
        }
        public int AddDevice(Device device)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            dbContext.Device.Add(device);
            dbContext.SaveChanges();
            return device.Id;
        }
        public IQueryable<Device> GetDevices()
        {
            DataContext dbContext = new DataContext(this._connectionString);
            return dbContext.Device;

        }
    }
}
