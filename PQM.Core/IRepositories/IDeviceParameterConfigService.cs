using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PQM.Core.DTOs;

namespace PQM.Core.IRepositories
{
    public interface IDeviceParameterConfigService
    {
        Task<DeviceConfigurationDto> GetDeviceConfigurationAsync(int deviceId, CancellationToken cancellationToken);
        Task<SaveConfigResultDto> SaveDeviceConfigurationAsync(int deviceId, List<int> parameterIds, CancellationToken cancellationToken);
        Task<List<SelectedParameterDto>> GetSelectedParametersAsync(int deviceId, CancellationToken cancellationToken);
    }
}
