using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;

namespace PQM.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DataContext _db;

        public DeviceRepository(DataContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<IEnumerable<Device>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Device
                .Include(d => d.MeterType)
                .Include(d => d.DeviceSyncSchedule)
                .Where(d => !d.IsDeleted && d.IsActive)
                .OrderBy(d => d.Id)
                .ToListAsync(cancellationToken);
        }
        public async Task<Device?> GetByIdAsync(int id,CancellationToken cancellationToken = default)
        {
            return await _db.Device
                .Include(d => d.MeterType)
                .FirstOrDefaultAsync(
                    d => d.Id == id && !d.IsDeleted,
                    cancellationToken);
        }
        public async Task<int> AddAsync(Device device,CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // Check duplicate fields
            var duplicateField = await GetDuplicateFieldAsync(
                device,
                cancellationToken);

            if (duplicateField != null)
            {
                throw new InvalidOperationException(
                    $"Device with the same {duplicateField} already exists.");
            }

            device.CreatedAt = DateTime.UtcNow;

            // Resolve MeterType by name if only the name was supplied
            if (device.MeterTypeId == null &&
                device.MeterType != null &&
                !string.IsNullOrWhiteSpace(device.MeterType.Name))
            {
                var meterType = await _db.Set<MeterType>()
                    .FirstOrDefaultAsync(
                        m => m.Name == device.MeterType.Name,
                        cancellationToken);

                if (meterType != null)
                {
                    device.MeterTypeId = meterType.Id;
                }
            }

            // Do not attach an existing navigation object accidentally
            device.MeterType = null;

            await _db.Device.AddAsync(
                device,
                cancellationToken);

            await _db.SaveChangesAsync(
                cancellationToken);

            return device.Id;
        }
        public async Task<bool> UpdateAsync(Device device,CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var existing = await _db.Device
                .FirstOrDefaultAsync(
                    d => d.Id == device.Id && !d.IsDeleted,
                    cancellationToken);

            if (existing == null)
                return false;

            // Check duplicate fields
            // Current device ID is automatically excluded
            var duplicateField = await GetDuplicateFieldAsync(
                device,
                cancellationToken);

            if (duplicateField != null)
            {
                throw new InvalidOperationException(
                    $"Another device with the same {duplicateField} already exists.");
            }

            existing.Name = device.Name;
            existing.IP = device.IP;
            existing.PORT = device.PORT;
            existing.SerialNumber = device.SerialNumber;
            existing.ConsumerNumber = device.ConsumerNumber;
            existing.IsActive = device.IsActive;
            existing.ClientAddress = device.ClientAddress;
            existing.ServerAddress = device.ServerAddress;
            existing.Authentication = device.Authentication;
            existing.Password = device.Password;
            existing.Timeout = device.Timeout;
            existing.TimeZoneId = device.TimeZoneId;
            existing.MeterTypeId = device.MeterTypeId;

            // Device -> Schedule
            existing.DeviceSyncScheduleId =
                device.DeviceSyncScheduleId;

            await _db.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        public async Task<bool> DeleteAsync(int id,CancellationToken cancellationToken = default)
        {
            var existing = await _db.Device.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted,cancellationToken);

            if (existing == null)
                return false;

            // Soft delete
            existing.IsDeleted = true;
            existing.IsActive = false;

            await _db.SaveChangesAsync(cancellationToken);

            return true;
        }
        public async Task<IEnumerable<MeterType>> GetMeterTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Set<MeterType>()
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }
        public async Task<string?> GetDuplicateFieldAsync(Device device,CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var query = _db.Device.Where(d => !d.IsDeleted);

            // During update, don't compare the device with itself
            if (device.Id > 0)
            {
                query = query.Where(d => d.Id != device.Id);
            }

            if (!string.IsNullOrWhiteSpace(device.Name))
            {
                bool exists = await query.AnyAsync(
                    d => d.Name == device.Name,
                    cancellationToken);

                if (exists)
                    return "Name";
            }

            if (!string.IsNullOrWhiteSpace(device.SerialNumber))
            {
                bool exists = await query.AnyAsync(
                    d => d.SerialNumber == device.SerialNumber,
                    cancellationToken);

                if (exists)
                    return "SerialNumber";
            }

            if (!string.IsNullOrWhiteSpace(device.ConsumerNumber))
            {
                bool exists = await query.AnyAsync(
                    d => d.ConsumerNumber == device.ConsumerNumber,
                    cancellationToken);

                if (exists)
                    return "ConsumerNumber";
            }

            if (!string.IsNullOrWhiteSpace(device.IP))
            {
                bool exists = await query.AnyAsync(
                    d => d.IP == device.IP,
                    cancellationToken);

                if (exists)
                    return "IP";
            }

            return null;
        }
    }
}