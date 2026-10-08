namespace PQM.Core.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // Schedule / Device
        public string Type { get; set; } = string.Empty;

        // Success / Info / Warning / Error
        public string Severity { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public User User { get; set; } = null!;
    }
}