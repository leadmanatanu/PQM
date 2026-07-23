using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PQM.Core.Entities;
using PQM.Core.DTOs;

namespace PQM.Core.IRepositories
{
    public interface IDeviceParameterConfigRepository
    {
        Task<Device?> GetDeviceByIdAsync(int deviceId, CancellationToken cancellationToken);
        Task<List<Parameter>> GetParametersForDeviceTypeAsync(string deviceTypeName, CancellationToken cancellationToken);
        Task<List<int>> GetSelectedParameterIdsAsync(int deviceId, CancellationToken cancellationToken);
        Task<List<Parameter>> GetParametersByIdsAsync(List<int> parameterIds, CancellationToken cancellationToken);
        Task<int> SaveConfigurationAsync(int deviceId, List<int> parameterIds, CancellationToken cancellationToken);
        Task<List<SelectedParameterDto>> GetSelectedParametersWithDetailsAsync(int deviceId, CancellationToken cancellationToken);
    }
}
