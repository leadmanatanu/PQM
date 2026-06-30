using PQM.Core.Entities;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class ParameterService : IParameterService
    {
        public string _connectionString { get; set; }

        public ParameterService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public IQueryable<Parameter> GetParameters()
        {
            DataContext dbContext = new DataContext(this._connectionString);
            return dbContext.Parameter.Where(x => x.IsActive && !x.IsDeleted);

        }

        public IQueryable<Parameter> GetParameters(int deviceId)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            var data = (from x in dbContext.Parameter.Where(x => x.IsActive && !x.IsDeleted)
                        select new Parameter
                        {
                            Id = x.Id,
                            Name = x.Name,
                            ObisCode = x.ObisCode,
                            ObjectType = x.ObjectType,
                            IsSelected = dbContext.DeviceParameterMapping.Any(y => y.DeviceId == deviceId && y.ParameterId == x.Id),
                        });


            return data;

        }
    }
}
