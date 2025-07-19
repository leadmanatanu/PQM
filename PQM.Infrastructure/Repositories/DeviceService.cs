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
            return dbContext.Device.Where(x => x.IsActive && !x.IsDeleted);
        }
        public bool UpdateDevice(Device device)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var deviceData = dbContext.Device.FirstOrDefault(x => x.Id == device.Id);
            if (deviceData == null)
            {
                return false;
            }
            deviceData.Name = device.Name;
            deviceData.SerialNumber = device.SerialNumber;
            deviceData.ConsumerNumber = device.ConsumerNumber;
            deviceData.IP = device.IP;
            deviceData.PORT = device.PORT;
            deviceData.FtpFolder = device.FtpFolder;
            deviceData.IsActive = device.IsActive;
            deviceData.IsDeleted = device.IsDeleted;
            deviceData.ModifiedDate = DateTime.UtcNow;
            dbContext.Device.Update(deviceData);
            dbContext.SaveChanges();
            return true;
        }

        public bool DeleteDevice(int id)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var deviceData = dbContext.Device.FirstOrDefault(x => x.Id == id);
            if (deviceData == null)
            {
                return false;
            }
            //dbContext.Device.Remove(deviceData);
            //dbContext.SaveChanges();
            deviceData.IsDeleted = true;
            dbContext.Device.Update(deviceData);
            dbContext.SaveChanges();
            return true;
        }

        public bool UpdateLastSync(int id, DateTime syncDate)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var deviceData = dbContext.Device.FirstOrDefault(x => x.Id == id);
            if (deviceData == null)
            {
                return false;
            }
            deviceData.LastSync = syncDate;
            dbContext.Device.Update(deviceData);
            dbContext.SaveChanges();
            return true;
        }
    }
}
