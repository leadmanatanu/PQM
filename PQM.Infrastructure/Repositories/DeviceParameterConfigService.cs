using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.DTOs;
using PQM.Core.IRepositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceParameterConfigService : IDeviceParameterConfigService
    {
        private readonly IDeviceParameterConfigRepository _repository;
        private readonly ILogger<DeviceParameterConfigService> _logger;

        public DeviceParameterConfigService(
            IDeviceParameterConfigRepository repository,
            ILogger<DeviceParameterConfigService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DeviceConfigurationDto> GetDeviceConfigurationAsync(int deviceId, CancellationToken cancellationToken)
        {
            var device = await _repository.GetDeviceByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                _logger.LogWarning("Failed to get device configuration: Device with ID {DeviceId} not found.", deviceId);
                throw new KeyNotFoundException($"Device with ID {deviceId} not found.");
            }

            var availableParameters = await _repository.GetParametersForDeviceTypeAsync(device.TypeName, cancellationToken);
            var selectedIds = await _repository.GetSelectedParameterIdsAsync(deviceId, cancellationToken);

            return new DeviceConfigurationDto
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                IP = device.IP,
                Port = device.PORT,
                DeviceType = device.TypeName,
                Status = device.Status,
                AvailableParameters = availableParameters.Select(p => new AvailableParameterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ObisCode = p.ObisCode,
                    ObjectType = p.ObjectType,
                    Attribute3 = p.Attribute3,
                    Scaler = p.Scaler,
                    Unit = p.Unit,
                    TypeName = p.TypeName
                }).ToList(),
                SelectedParameterIds = selectedIds
            };
        }

        public async Task<SaveConfigResultDto> SaveDeviceConfigurationAsync(int deviceId, List<int> parameterIds, CancellationToken cancellationToken)
        {
            parameterIds ??= new List<int>();

            // 1. Validate device exists
            var device = await _repository.GetDeviceByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                _logger.LogWarning("Failed to save device configuration: Device with ID {DeviceId} not found.", deviceId);
                throw new KeyNotFoundException($"Device with ID {deviceId} not found.");
            }

            // 2. Validate all parameter IDs exist
            var distinctInputIds = parameterIds.Distinct().ToList();
            var parameters = await _repository.GetParametersByIdsAsync(distinctInputIds, cancellationToken);

            if (parameters.Count != distinctInputIds.Count)
            {
                var foundIds = parameters.Select(p => p.Id).ToHashSet();
                var missingIds = distinctInputIds.Where(id => !foundIds.Contains(id)).ToList();
                var missingIdsStr = string.Join(", ", missingIds);
                _logger.LogWarning("Validation failed: Device {DeviceId} configuration save request contains invalid parameter IDs: {MissingIds}", deviceId, missingIdsStr);
                throw new ArgumentException($"One or more parameter IDs are invalid: {missingIdsStr}");
            }

            // 3. Validate every parameter's TypeName is allowed for this device's TypeName
            if (string.Equals(device.TypeName, "PQ", StringComparison.OrdinalIgnoreCase))
            {
                var invalidParams = parameters.Where(p => !string.Equals(p.TypeName, "PQ", StringComparison.OrdinalIgnoreCase)).ToList();
                if (invalidParams.Any())
                {
                    var invalidNames = string.Join(", ", invalidParams.Select(p => $"'{p.Name}' ({p.TypeName})"));
                    _logger.LogWarning("Validation failed: PQ device {DeviceId} cannot have non-PQ parameters: {Params}", deviceId, invalidNames);
                    throw new ArgumentException($"One or more parameters are not allowed for PQ devices: {invalidNames}");
                }
            }
            else if (string.Equals(device.TypeName, "ABT", StringComparison.OrdinalIgnoreCase))
            {
                var invalidParams = parameters.Where(p => !string.Equals(p.TypeName, "ABT", StringComparison.OrdinalIgnoreCase)).ToList();
                if (invalidParams.Any())
                {
                    var invalidNames = string.Join(", ", invalidParams.Select(p => $"'{p.Name}' ({p.TypeName})"));
                    _logger.LogWarning("Validation failed: ABT device {DeviceId} cannot have non-ABT parameters: {Params}", deviceId, invalidNames);
                    throw new ArgumentException($"One or more parameters are not allowed for ABT devices: {invalidNames}");
                }
            }
            // If device.TypeName is BOTH, any parameter TypeName (PQ, ABT, BOTH) is allowed.

            // 4. Perform save inside transaction
            try
            {
                int savedCount = await _repository.SaveConfigurationAsync(deviceId, distinctInputIds, cancellationToken);
                _logger.LogInformation("Successfully saved configuration for Device {DeviceId}. Parameter count: {Count}", deviceId, savedCount);
                
                return new SaveConfigResultDto
                {
                    Success = true,
                    Message = "Device configuration saved successfully.",
                    SavedCount = savedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error saving configuration for Device {DeviceId}.", deviceId);
                throw;
            }
        }

        public async Task<List<SelectedParameterDto>> GetSelectedParametersAsync(int deviceId, CancellationToken cancellationToken)
        {
            var device = await _repository.GetDeviceByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                _logger.LogWarning("Failed to get selected parameters: Device with ID {DeviceId} not found.", deviceId);
                throw new KeyNotFoundException($"Device with ID {deviceId} not found.");
            }

            return await _repository.GetSelectedParametersWithDetailsAsync(deviceId, cancellationToken);
        }
    }
}
