using PQM.Core.Entities;

namespace PQM.Core.IRepositories
{
    public interface IParameterService
    {
        IQueryable<Parameter> GetParameters();
        IQueryable<Parameter> GetParameters(int deviceId);
    }
}
