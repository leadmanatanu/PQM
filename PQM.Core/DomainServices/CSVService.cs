using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualBasic.FileIO;
using PQM.Core.Entities;
using PQM.Core.Helper;
using PQM.Core.IRepositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.DomainServices
{
    public class CSVService : ICSVService
    {
        public List<DeviceLog> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParatmeter)
        {
            List<DeviceLog> logList = new List<DeviceLog>();
            using (TextFieldParser parser = new TextFieldParser(csvFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                List<string> lstHeader = new List<string>();
                List<string> lstAllHeader = new List<string>();
                int counter = 0;
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    int innerCounter = 0;
                    int matchedHeaderCounter = 0;
                    DateTime dateStamp = new DateTime();
                    foreach (var field in fields)
                    {
                        if (counter == 0)
                        {
                            lstAllHeader.Add(field);
                            if (mappedParatmeter.Contains(field)) // pick mapped parameters of device
                            {
                                lstHeader.Add(field);
                            }
                        }
                        else
                        {
                            if (innerCounter == 0) // pick DateStamp
                            {
                                dateStamp = Convert.ToDateTime(field);
                                //dateStamp = DateTime.ParseExact(field, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                //This logic works if csv file order is sequential eg. 1,2,3,4 etc
                                //if (lstHeader.Contains(innerCounter.ToString()))
                                //{
                                //    DeviceLog log = new DeviceLog();
                                //    log.DateStamp = dateStamp;
                                //    log.DeviceId = deviceId;
                                //    log.ParameterId = innerCounter;
                                //    log.Value = field;
                                //    logList.Add(log);
                                //}

                                //This logic will work for all types order
                                string header = lstAllHeader[innerCounter];
                                if (matchedHeaderCounter > lstHeader.Count - 1)
                                    break;
                                string matchedHeader = lstHeader[matchedHeaderCounter];
                                if (header == matchedHeader)
                                {
                                    matchedHeaderCounter++;
                                    DeviceLog log = new DeviceLog();
                                    log.DateStamp = dateStamp;
                                    log.DeviceId = deviceId;
                                    log.ParameterId = Convert.ToInt32(matchedHeader);
                                    log.Value = field;
                                    logList.Add(log);
                                }
                            }
                            innerCounter++;
                        }
                    }
                    counter++;
                }
            }
            return logList;
        }

        public List<EventLog> ReadEventLog(int deviceId, string eventType, string csvFilePath)
        {
            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,   // ignore missing fields
                HeaderValidated = null      // don't validate headers against class
            });

            return eventType switch
            {
                nameof(EventType.dip) => MapEvents<DipEvent>(csv, deviceId, eventType, (src, dst) => dst.Min_Voltage = src.Min_Voltage),
                nameof(EventType.interrupt) => MapEvents<InterruptEvent>(csv, deviceId, eventType),
                nameof(EventType.rvc) => MapEvents<RVCEvent>(csv, deviceId, eventType, (src, dst) => { dst.UMAX = src.UMAX; dst.USS = src.USS; }),
                nameof(EventType.swell) => MapEvents<SwellEvent>(csv, deviceId, eventType, (src, dst) => dst.Max_Voltage = src.Max_Voltage),
                nameof(EventType.shortflicker) or nameof(EventType.longflicker) => MapEvents<FlickerEvent>(csv, deviceId, eventType, (src, dst) => { dst.Date = src.Date; dst.A = src.A; dst.B = src.B; dst.C = src.C; }),
                _ => new List<EventLog>()
            };
        }

        private List<EventLog> MapEvents<T>(CsvReader csv, int deviceId, string eventType, Action<T, EventLog> extraMapping = null)
            where T : IBaseEvent
        {
            var list = new List<EventLog>();

            foreach (var item in csv.GetRecords<T>())
            {
                var log = new EventLog
                {
                    DeviceId = deviceId,
                    EventType = eventType,
                    CreatedDate = DateTime.UtcNow,
                    Start_Time = item.Start_Time,
                    End_Time = item.End_Time,
                    Phase = item.Phase,
                    Duration = item.Duration
                };

                extraMapping?.Invoke(item, log);
                list.Add(log);
            }

            return list;
        }
    }
}
