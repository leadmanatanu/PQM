using System;

namespace PQM.Core.Entities
{
    public class DeviceSyncRequest
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        public string? ErrorMessage { get; set; }

        public virtual Device? Device { get; set; }
    }
}
