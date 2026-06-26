using PQM.Core.Entities;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceParameterService : IDeviceParameterService
    {
        public string _connectionString { get; set; }

        public DeviceParameterService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool AddDeviceParameterMapping(List<DeviceParameterMapping> data)
        {
            var firstItem = data.FirstOrDefault();
            if (firstItem == null)
                return false;
            int deviceId = firstItem.DeviceId;
            data.ForEach(x => { x.DateStamp = DateTime.UtcNow; });
            using (var dbContext = new DataContext(this._connectionString))
            {
                var mappingData = dbContext.DeviceParameterMapping.Where(x => x.DeviceId == deviceId);
                var existingItems = mappingData
                    .Select(x => new { x.DeviceId, x.ParameterId })
                    .ToList();

                var newItems = data
                    .Where(d => !existingItems.Any(e => e.DeviceId == d.DeviceId && e.ParameterId == d.ParameterId))
                    .ToList();

                var deletedItems = mappingData.ToList()
                    .Where(d => !data.Any(e => e.DeviceId == d.DeviceId && e.ParameterId == d.ParameterId))
                    .ToList();

                if (newItems.Any())
                {
                    dbContext.DeviceParameterMapping.AddRange(newItems);
                    dbContext.SaveChanges();
                }

                if (deletedItems.Any())
                {
                    dbContext.DeviceParameterMapping.RemoveRange(deletedItems);
                    dbContext.SaveChanges();
                }
            }
            return true;
        }

        public IQueryable<DeviceParameterMapping> GetDeviceParameterMapping(int deviceId)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var data = (from x in dbContext.DeviceParameterMapping.Where(x => x.DeviceId == deviceId)
                        select new DeviceParameterMapping
                        {
                            Id = x.Id,
                            DeviceId = deviceId,
                            ParameterId = x.ParameterId
                        });
            return data;

        }
    }
}
