using System;

namespace PQM.Core.DTOs
{
    public class DeviceStatusChangedDto
    {
        public int DeviceId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? LastSync { get; set; }
        public string? LastError { get; set; }
    }
}
