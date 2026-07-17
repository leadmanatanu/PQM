using PQM.Core.Entities;
using System.Collections.Generic;

namespace PQM.Core.IRepositories
{
    public interface ICSVService
    {
        List<ParameterValue> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParatmeter);
        List<Event> ReadEventLog(int deviceId, string eventType, string csvFilePath);
    }
}
