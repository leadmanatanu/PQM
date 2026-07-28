using PQM.Core.Entities;
using System.Collections.Generic;

namespace PQM.Core.IRepositories
{
    public interface ICSVService
    {
        List<ReadingValue> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParameter);
        List<DeviceEvent> ReadEventLog(int deviceId, string eventType, string csvFilePath);
    }
}
