using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PQM.Core.DTOs;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IParameterRepository
    {
        Task<IEnumerable<Parameter>> GetVisibleParametersAsync(int? deviceId = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<object>> GetDeviceLatestReadingsAsync(int deviceId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ParameterValueSearchDto> Items, int TotalCount)> SearchReadingsAsync(
            int deviceId,
            int parameterId,
            DateTime startDate,
            DateTime endDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
