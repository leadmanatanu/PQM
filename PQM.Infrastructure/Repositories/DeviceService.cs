using PQM.Core.Entities;
using PQM.Core.IRepositories;
using Microsoft.EntityFrameworkCore;
using PQM.Core.DTOs;
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


        public IQueryable<DeviceDto> GetDevices()
        {
            DataContext dbContext = new DataContext(this._connectionString);

            return dbContext.Device
                .Include(d => d.MeterType)
                .Where(x => !x.IsDeleted)
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    Name = d.Name,

                    MeterTypeId = d.MeterTypeId,

                    MeterTypeName = d.MeterType != null
                        ? d.MeterType.Name
                        : null,

                    IP = d.IP,
                    PORT = d.PORT,

                    SerialNumber = d.SerialNumber,
                    ConsumerNumber = d.ConsumerNumber,

                    IsActive = d.IsActive,

                    Status = d.Status,

                    LastSync = d.LastSync
                });
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
            deviceData.UserId = device.UserId;
            deviceData.SerialNumber = device.SerialNumber;
            deviceData.ConsumerNumber = device.ConsumerNumber;
            deviceData.IP = device.IP;
            deviceData.PORT = device.PORT;
            deviceData.IsActive = device.IsActive;
            deviceData.IsDeleted = device.IsDeleted;
            deviceData.ClientAddress = device.ClientAddress;
            deviceData.ServerAddress = device.ServerAddress;
            deviceData.Authentication = device.Authentication;
            deviceData.Password = device.Password;
            deviceData.Timeout = device.Timeout;
            deviceData.TimeZoneId = device.TimeZoneId;
            deviceData.MeterTypeId = device.MeterTypeId;
            deviceData.ModifiedDate = DateTime.UtcNow;
            dbContext.Device.Update(deviceData);
            dbContext.SaveChanges();
            return true;
        }

        public bool DeleteDevice(int id)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var deviceData = dbContext.Device.FirstOrDefault(x =>x.Id == id);
            if (deviceData == null)
            {
                return false;
            }
            deviceData.IsDeleted = true;
            deviceData.ModifiedDate = DateTime.UtcNow;
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
