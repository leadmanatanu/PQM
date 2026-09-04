namespace PQM.Server.Models
{
    public class UpdateScheduleRequest
    {
        public bool IsEnabled { get; set; }
        public string ScheduledTime { get; set; } = "00:00";
        public string RepeatMode { get; set; } = "Daily";
    }
}
