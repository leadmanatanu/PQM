using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class SyncDeviceRunLogs
    {
        [Key]
        public int Id { get; set; }

        public int RunId { get; set; }

        public int DeviceId { get; set; }

        public string? IP { get; set; }

        public int? Port { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long? DurationMs { get; set; }

        public string Outcome { get; set; } = string.Empty;

        public int ProfilesAttempted { get; set; }

        public int ProfilesSucceeded { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ExceptionDetails { get; set; }
        public SyncRunLogs? Run { get; set; }
        public Device? Device { get; set; }
    }
}