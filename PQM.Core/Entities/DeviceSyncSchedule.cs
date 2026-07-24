using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    public class DeviceSyncSchedule
    {
        [Key]
        [ForeignKey("Device")]
        public int DeviceId { get; set; }

        public bool IsEnabled { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string RepeatMode { get; set; } = "Daily";
        public DateTime? NextRunAtUtc { get; set; }
        public DateTime? LastRunAtUtc { get; set; }
        public string? LastRunStatus { get; set; }
    }
}
