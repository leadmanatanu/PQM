using System;

namespace PQM.Core.Entities
{
    public class DeviceEvent
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public DateTime EventTime { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int EventCode { get; set; }
        public string? RawClock { get; set; }
        public string? RawValue { get; set; }
        public DateTime ReadTime { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Parameter? Parameter { get; set; }
    }
}
