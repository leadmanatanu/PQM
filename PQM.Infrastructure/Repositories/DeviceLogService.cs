using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using System.ComponentModel.DataAnnotations.Schema;

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

        public (List<DeviceLogSearch>, int) GetDeviceLogs(int deviceId, int parameterId, int pageNumber, int pageSize, DateTime startDate, DateTime endDate)
        {
            int skip = (pageNumber - 1) * pageSize;
            var totalCountParam = new SqlParameter
            {
                ParameterName = "@TotalCount",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            DataContext context = new DataContext(this._connectionString);
            var query = context.Set<DeviceLogSearch>().FromSqlRaw("EXEC GetDeviceLogs @DeviceId, @ParameterId, @Skip, @Take, @StartDate, @EndDate, @TotalCount OUTPUT",
                        new SqlParameter("@DeviceId", deviceId),
                        new SqlParameter("@ParameterId", parameterId),
                        new SqlParameter("@Skip", skip),
                        new SqlParameter("@Take", pageSize),
                        new SqlParameter("@StartDate", startDate),
                        new SqlParameter("@EndDate", endDate),
                        totalCountParam).AsNoTracking()
                .ToList();
            int totalCount = (int)totalCountParam.Value;
            return (query, totalCount);
        }

        public bool AddDeviceEventLogs(List<EventLog> eventLogs)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false; //Disable Change Tracking during bulk insert
            dbContext.BulkInsert(eventLogs);
            return true;
        }
    }
}
