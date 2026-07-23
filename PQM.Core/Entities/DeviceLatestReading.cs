using System;

namespace PQM.Core.Entities
{
    public class DeviceLatestReading
    {
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public string? Value { get; set; }
        public string? RawValue { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Parameter? Parameter { get; set; }
    }
}
