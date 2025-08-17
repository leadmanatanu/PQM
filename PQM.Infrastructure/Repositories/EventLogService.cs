using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace PQM.Infrastructure.Repositories
{
    public class EventLogService : IEventLogService
    {
        public string _connectionString { get; set; }

        public EventLogService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public IQueryable<EventLog> GetEventLogs()
        {
            DataContext dbContext = new DataContext(this._connectionString);
            return dbContext.EventLog;
        }

        public (List<EventLog>, int) GetEventLogs(int deviceId, string eventType, int pageNumber, int pageSize, DateTime startDate, DateTime endDate)
        {
            int skip = (pageNumber - 1) * pageSize;
            var totalCountParam = new SqlParameter
            {
                ParameterName = "@TotalCount",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            DataContext context = new DataContext(this._connectionString);
            var query = context.Set<EventLog>().FromSqlRaw("EXEC GetEventLogs @DeviceId, @EventType, @Skip, @Take, @StartDate, @EndDate, @TotalCount OUTPUT",
                        new SqlParameter("@DeviceId", deviceId),
                        new SqlParameter("@EventType", eventType),
                        new SqlParameter("@Skip", skip),
                        new SqlParameter("@Take", pageSize),
                        new SqlParameter("@StartDate", startDate),
                        new SqlParameter("@EndDate", endDate),
                        totalCountParam).AsNoTracking()
                .ToList();
            int totalCount = (int)totalCountParam.Value;
            return (query, totalCount);
        }
    }
}
