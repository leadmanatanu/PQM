namespace PQM.Core.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredAt { get; }
    }

    public class DeviceSyncCompletedEvent : IDomainEvent
    {
        public int DeviceId { get; set; }
        public DateTime LastSyncAt { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    public interface IEventPublisher
    {
        Task PublishAsync(IDomainEvent @event);
    }

    public interface IEventHandler<TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent @event);
    }
}