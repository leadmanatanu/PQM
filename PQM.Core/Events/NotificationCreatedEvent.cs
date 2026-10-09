
using PQM.Core.Events;

namespace PQM.Core.Events
{
    public class NotificationCreatedEvent : IDomainEvent
    {
        public int NotificationId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }


        public List<int> RecipientUserIds { get; set; } = new();

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
