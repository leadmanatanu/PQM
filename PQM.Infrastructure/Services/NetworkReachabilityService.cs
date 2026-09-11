using System.Net.Sockets;

namespace PQM.Infrastructure.Services
{
    public interface INetworkReachabilityService
    {
        Task<bool> IsReachableAsync(string ip, int port, int timeoutMs, CancellationToken ct);
    }

    public class NetworkReachabilityService : INetworkReachabilityService
    {
        public async Task<bool> IsReachableAsync(string ip, int port, int timeoutMs, CancellationToken ct)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutMs, ct);

                var completed = await Task.WhenAny(connectTask, timeoutTask);
                if (completed != connectTask) return false;

                await connectTask;
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}