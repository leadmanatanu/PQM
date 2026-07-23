using System;
using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class DeviceConnectionEvent
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string? ErrorDetails { get; set; }
        public bool IsResolved { get; set; }
        public Device? Device { get; set; }
    }
}
