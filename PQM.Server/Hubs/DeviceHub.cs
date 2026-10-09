using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace PQM.Server.Hubs
{
    public class DeviceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, HashSet<int>> _subscriptions = new();
        private static readonly ConcurrentDictionary<string, int> _userConnections = new();

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
        public Task RegisterUser(int userId)
        {
            if (userId <= 0)
                throw new HubException("Invalid user ID.");

            _userConnections[Context.ConnectionId] = userId;

            return Task.CompletedTask;
        }

        public static IReadOnlyList<string> GetUserConnections(int userId)
        {
            return _userConnections
                .Where(x => x.Value == userId)
                .Select(x => x.Key)
                .ToList();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _subscriptions.TryRemove(Context.ConnectionId, out _);
            _userConnections.TryRemove(Context.ConnectionId, out _);

            return base.OnDisconnectedAsync(exception);
        }

        public static IReadOnlyList<string> GetSubscribedConnections(int deviceId)
        {
            return _subscriptions
                .Where(kvp =>
                {
                    lock (kvp.Value)
                    {
                        return kvp.Value.Contains(deviceId);
                    }
                })
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}