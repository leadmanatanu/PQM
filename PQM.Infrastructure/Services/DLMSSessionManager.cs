using System;
using System.Collections.Concurrent;

namespace PQM.Infrastructure.Services
{
    public class DLMSSessionManager
    {
        private readonly ConcurrentDictionary<int, DLMSReader> _sessions = new();

        public DLMSReader? GetSession(int deviceId)
        {
            if (_sessions.TryGetValue(deviceId, out var reader))
                return reader;
            return null;
        }

        public DLMSReader Connect(
            int deviceId,
            string ipAddress,
            int port,
            int clientAddress = 16,
            int serverAddress = 1,
            Gurux.DLMS.Enums.Authentication authentication = Gurux.DLMS.Enums.Authentication.None,
            string password = "",
            bool useLogicalNameReferencing = true,
            Gurux.DLMS.Enums.Standard standard = Gurux.DLMS.Enums.Standard.DLMS,
            Gurux.DLMS.Enums.InterfaceType interfaceType = Gurux.DLMS.Enums.InterfaceType.WRAPPER,
            int waitTimeMs = 5000,
            int retryCount = 3)
        {
            // Disconnect any existing session first and give the meter time to release it.
            // DLMS meters are single-connection devices — without a short delay after
            // disconnect the meter may still be processing the close when we reconnect,
            // causing a TCP-level "connection refused / timed out" error.
            bool hadExistingSession = _sessions.ContainsKey(deviceId);
            Disconnect(deviceId);
            if (hadExistingSession)
            {
                System.Threading.Thread.Sleep(2000); // 2s grace period for meter to release session
            }

            var reader = new DLMSReader(
                ipAddress, port,
                clientAddress, serverAddress,
                authentication, password,
                useLogicalNameReferencing, standard, interfaceType);

            // Apply per-device timing settings from ConnectionSettings
            reader.WaitTime = waitTimeMs;
            reader.RetryCount = retryCount;

            try
            {
                reader.Connect();
                _sessions[deviceId] = reader;
                return reader;
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }

        public void Touch(int deviceId) { /* no-op: kept for compatibility */ }

        public void Disconnect(int deviceId)
        {
            if (_sessions.TryRemove(deviceId, out var reader))
            {
                try { reader.Disconnect(); reader.Dispose(); } catch { }
            }
        }
    }
}
