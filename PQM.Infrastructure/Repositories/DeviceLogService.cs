using EFCore.BulkExtensions;
using PQM.Core.Entities;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceLogService : IDeviceLogService
    {
        public string _connectionString { get; set; }

        public DeviceLogService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool AddDeviceLogs(List<DeviceLog> deviceLogs)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            dbContext.DeviceLog.AddRange(deviceLogs);
            dbContext.SaveChanges();
            return true;
        }

        public IQueryable<DeviceLog> GetDeviceLogs()
        {
            DataContext dbContext = new DataContext(this._connectionString);
            return dbContext.DeviceLog;
        }

        public bool AddBulkDeviceLogs(List<DeviceLog> deviceLogs)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false; //Disable Change Tracking during bulk insert
            dbContext.BulkInsert(deviceLogs);
            return true;
        }
    }
}
