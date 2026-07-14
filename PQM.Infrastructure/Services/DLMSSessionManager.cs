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
            Gurux.DLMS.Enums.InterfaceType interfaceType = Gurux.DLMS.Enums.InterfaceType.WRAPPER)
        {
            // Disconnect any existing session first
            Disconnect(deviceId);

            var reader = new DLMSReader(
                ipAddress, port,
                clientAddress, serverAddress,
                authentication, password,
                useLogicalNameReferencing, standard, interfaceType);

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
