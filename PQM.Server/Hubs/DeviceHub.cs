using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace PQM.Server.Hubs
{
    public class DeviceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, HashSet<int>> _subscriptions = new();

        public Task SubscribeToDevices(List<int> deviceIds)
        {
            var set = _subscriptions.GetOrAdd(Context.ConnectionId, _ => new HashSet<int>());
            lock (set)
            {
                foreach (var id in deviceIds) set.Add(id);
            }
            return Task.CompletedTask;
        }

        public Task UnsubscribeFromDevices(List<int> deviceIds)
        {
            if (_subscriptions.TryGetValue(Context.ConnectionId, out var set))
            {
                lock (set)
                {
                    foreach (var id in deviceIds) set.Remove(id);
                }
            }
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _subscriptions.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public static IReadOnlyList<string> GetSubscribedConnections(int deviceId)
        {
            return _subscriptions
                .Where(kvp => kvp.Value.Contains(deviceId))
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}