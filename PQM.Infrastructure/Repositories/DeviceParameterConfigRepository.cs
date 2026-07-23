using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.DTOs;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceParameterConfigRepository : IDeviceParameterConfigRepository
    {
        private readonly DataContext _context;

        public DeviceParameterConfigRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<Device?> GetDeviceByIdAsync(int deviceId, CancellationToken cancellationToken)
        {
            return await _context.Device
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted, cancellationToken);
        }

        public async Task<List<Parameter>> GetParametersForDeviceTypeAsync(string deviceTypeName, CancellationToken cancellationToken)
        {
            var query = _context.Parameter.AsQueryable();

            if (string.Equals(deviceTypeName, "PQ", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.TypeName == "PQ");
            }
            else if (string.Equals(deviceTypeName, "ABT", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.TypeName == "ABT");
            }
            // For BOTH, all parameters (PQ, ABT, BOTH) are allowed, so no filtering by TypeName is applied.

            return await query
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetSelectedParameterIdsAsync(int deviceId, CancellationToken cancellationToken)
        {
            return await _context.DeviceParameterConfig
                .Where(c => c.DeviceId == deviceId && c.IsSelected)
                .Select(c => c.ParameterId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Parameter>> GetParametersByIdsAsync(List<int> parameterIds, CancellationToken cancellationToken)
        {
            return await _context.Parameter
                .Where(p => parameterIds.Contains(p.Id) && p.IsActive && !p.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> SaveConfigurationAsync(int deviceId, List<int> parameterIds, CancellationToken cancellationToken)
        {
            // Wrap delete and insert in a transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete previous selections for this device
                var existingConfigs = await _context.DeviceParameterConfig
                    .Where(c => c.DeviceId == deviceId)
                    .ToListAsync(cancellationToken);

                if (existingConfigs.Any())
                {
                    _context.DeviceParameterConfig.RemoveRange(existingConfigs);
                }

                // Deduplicate parameterIds and create new configuration rows
                var distinctParamIds = parameterIds.Distinct().ToList();
                var now = DateTime.UtcNow;
                
                var newConfigs = distinctParamIds.Select(paramId => new DeviceParameterConfig
                {
                    DeviceId = deviceId,
                    ParameterId = paramId,
                    IsSelected = true,
                    LastModifiedDate = now
                }).ToList();

                if (newConfigs.Any())
                {
                    await _context.DeviceParameterConfig.AddRangeAsync(newConfigs, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return newConfigs.Count;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<List<SelectedParameterDto>> GetSelectedParametersWithDetailsAsync(int deviceId, CancellationToken cancellationToken)
        {
            var query = from config in _context.DeviceParameterConfig
                        join param in _context.Parameter on config.ParameterId equals param.Id
                        where config.DeviceId == deviceId && config.IsSelected && param.IsActive && !param.IsDeleted
                        select new SelectedParameterDto
                        {
                            ParameterId = param.Id,
                            Name = param.Name,
                            ObisCode = param.ObisCode,
                            ObjectType = param.ObjectType,
                            Attribute3 = param.Attribute3,
                            Scaler = param.Scaler,
                            Unit = param.Unit,
                            TypeName = param.TypeName,
                            LastModifiedDate = config.LastModifiedDate
                        };

            return await query.ToListAsync(cancellationToken);
        }
    }
}
