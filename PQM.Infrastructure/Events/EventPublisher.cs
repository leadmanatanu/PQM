using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PQM.Core.Events;
using System.Text.Json;

namespace PQM.Infrastructure.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventPublisher> _logger;
        public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        public async Task PublishAsync(IDomainEvent @event)
        {
            _logger.LogInformation(
                "[EventPublisher] EVENT DATA: {EventData}",
                JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            await using var scope = _serviceProvider.CreateAsyncScope();

            var eventType = @event.GetType();

            var handlerType =
                typeof(IEventHandler<>).MakeGenericType(eventType);

            var handlers = scope.ServiceProvider
                .GetServices(handlerType);

            _logger.LogInformation(
                "[EventPublisher] EventType={EventType}, HandlerType={HandlerType}, HandlerCount={HandlerCount}",
                eventType.FullName,
                handlerType.FullName,
                handlers.Count());

            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod("HandleAsync");

                if (method != null)
                {
                    _logger.LogInformation(
                        "[EventPublisher] Invoking handler: {HandlerName}. EventData: {EventData}",
                        handler.GetType().FullName,
                        JsonSerializer.Serialize(@event, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                    try
                    {
                        var task = (Task?)method.Invoke(
                            handler, new[] { @event });

                        if (task != null)
                        {
                            tasks.Add(task);

                            _logger.LogInformation(
                                "[EventPublisher] Handler execution started. HandlerName={HandlerName}",
                                handler.GetType().FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[EventPublisher] Failed to invoke handler. HandlerName={HandlerName}, EventType={EventType}",
                            handler.GetType().FullName,
                            eventType.FullName);

                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[EventPublisher] HandleAsync method not found for handler {HandlerName}",
                        handler.GetType().FullName);
                }
            }

            _logger.LogInformation(
                "[EventPublisher] Total handler tasks started: {TaskCount}",
                tasks.Count);

            try
            {
                await Task.WhenAll(tasks);

                _logger.LogInformation(
                    "[EventPublisher] All handlers completed successfully. EventType={EventType}",
                    eventType.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[EventPublisher] Error publishing event. EventType={EventType}, EventData={EventData}",
                    eventType.FullName,
                    JsonSerializer.Serialize(@event, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
            }
        }
    }

}