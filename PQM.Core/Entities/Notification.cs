namespace PQM.Core.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty; // Schedule / Device

        public string Severity { get; set; } = string.Empty; // Success / Info / Warning / Error

        public DateTime CreatedAt { get; set; }
    }
}