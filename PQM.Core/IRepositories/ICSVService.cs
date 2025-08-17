using PQM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.IRepositories
{
    public interface ICSVService
    {
        List<DeviceLog> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParatmeter);
        List<EventLog> ReadEventLog(int deviceId, string eventType, string csvFilePath);
    }
}
