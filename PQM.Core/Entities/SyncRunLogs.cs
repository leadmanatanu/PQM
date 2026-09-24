using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class SyncRunLogs
    {
        [Key]
        public int Id { get; set; }

        public int ScheduleId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long? DurationMs { get; set; }

        public string Status { get; set; } = SyncRunStatus.Running;

        public int TotalDevices { get; set; }

        public int SucceededDevices { get; set; }

        public int FailedDevices { get; set; }

        public DateTime? NextRunAt { get; set; }

        public string? ErrorMessage { get; set; }

        public DeviceSyncSchedule? Schedule { get; set; }

        public ICollection<SyncDeviceRunLogs> DeviceRuns { get; set; }
            = new List<SyncDeviceRunLogs>();
    }

    public static class SyncRunStatus
    {
        public const string Running = "Running";
        public const string Success = "Success";
        public const string PartialSuccess = "PartialSuccess";
        public const string Failed = "Failed";
    }
}