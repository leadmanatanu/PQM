using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                .Where(d => !d.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Device
                .Include(d => d.MeterType)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        }

        public async Task<int> AddAsync(Device device, CancellationToken cancellationToken = default)
        {
            device.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(device.Status))
            {
                device.Status = "Offline";
            }

            if (device.MeterTypeId == null && device.MeterType != null && !string.IsNullOrWhiteSpace(device.MeterType.Name))
            {
                var mt = await _db.Set<MeterType>()
                    .FirstOrDefaultAsync(m => m.Name == device.MeterType.Name, cancellationToken);
                if (mt != null)
                {
                    device.MeterTypeId = mt.Id;
                }
            }

            device.MeterType = null!;
            await _db.Device.AddAsync(device, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return device.Id;
        }

        public async Task<bool> UpdateAsync(Device device, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Device.FirstOrDefaultAsync(d => d.Id == device.Id && !d.IsDeleted, cancellationToken);
            if (existing == null) return false;

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

            if (device.MeterTypeId.HasValue)
            {
                existing.MeterTypeId = device.MeterTypeId;
            }
            else if (device.MeterType != null && !string.IsNullOrWhiteSpace(device.MeterType.Name))
            {
                var mt = await _db.Set<MeterType>()
                    .FirstOrDefaultAsync(m => m.Name == device.MeterType.Name, cancellationToken);
                if (mt != null)
                {
                    existing.MeterTypeId = mt.Id;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }


        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Device.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
            if (existing == null) return false;

            existing.IsDeleted = true;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> EnableSyncAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Device.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
            if (existing == null) return false;

            existing.IsActive = true;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DisableSyncAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Device.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
            if (existing == null) return false;

            existing.IsActive = false;
            existing.Status = "Offline";
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task QueueSyncRequestAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var req = new DeviceSyncRequest
            {
                DeviceId = deviceId,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending"
            };
            await _db.DeviceSyncRequests.AddAsync(req, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<DeviceSyncHistory>> GetSyncHistoryAsync(int deviceId, int take = 50, CancellationToken cancellationToken = default)
        {
            return await _db.DeviceSyncHistories
                .Where(h => h.DeviceId == deviceId)
                .OrderByDescending(h => h.StartedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<DeviceSyncSchedule?> GetScheduleAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            return await _db.DeviceSyncSchedules
                .FirstOrDefaultAsync(s => s.Id == deviceId, cancellationToken);
        }

        public async Task UpsertScheduleAsync(DeviceSyncSchedule schedule, CancellationToken cancellationToken = default)
        {
            var existing = await _db.DeviceSyncSchedules
                .FirstOrDefaultAsync(s => s.Id == schedule.Id, cancellationToken);

            if (existing == null)
            {
                await _db.DeviceSyncSchedules.AddAsync(schedule, cancellationToken);
            }
            else
            {
                existing.IsEnabled = schedule.IsEnabled;
                existing.ScheduledTime = schedule.ScheduledTime;
                existing.RepeatMode = schedule.RepeatMode;
                existing.NextRunAtUtc = schedule.NextRunAtUtc;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
