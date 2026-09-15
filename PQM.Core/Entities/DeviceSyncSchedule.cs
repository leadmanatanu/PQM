using System;
using System.ComponentModel.DataAnnotations;
namespace PQM.Core.Entities
{
    public class DeviceSyncSchedule
    {
        [Key]
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string RepeatMode { get; set; } = "Daily";
        public DateTime? NextRunAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public string? LastRunStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}
