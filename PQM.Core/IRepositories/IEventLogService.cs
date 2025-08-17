using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IEventLogService
    {
        IQueryable<EventLog> GetEventLogs();
        (List<EventLog>, int) GetEventLogs(int deviceId, string eventType, int pageNumber, int pageSize, DateTime startDate, DateTime endDate);
    }
}
