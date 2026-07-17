using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualBasic.FileIO;
using PQM.Core.Entities;
using PQM.Core.Helper;
using PQM.Core.IRepositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PQM.Core.DomainServices
{
    public class CSVService : ICSVService
    {
        public List<ParameterValue> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParameter)
        {
            List<ParameterValue> logList = new List<ParameterValue>();
            using (TextFieldParser parser = new TextFieldParser(csvFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                List<string> lstHeader = new List<string>();
                List<string> lstAllHeader = new List<string>();
                int counter = 0;
                while (!parser.EndOfData)
                {
                    string[]? fields = parser.ReadFields();
                    if (fields == null)
                        break;
                    int innerCounter = 0;
                    int matchedHeaderCounter = 0;
                    DateTime dateStamp = new DateTime();
                    foreach (var field in fields)
                    {
                        if (counter == 0)
                        {
                            lstAllHeader.Add(field);
                            if (mappedParameter.Contains(field))
                            {
                                lstHeader.Add(field);
                            }
                        }
                        else
                        {
                            if (innerCounter == 0)
                            {
                                dateStamp = Convert.ToDateTime(field);
                            }
                            else
                            {
                                string header = lstAllHeader[innerCounter];
                                if (matchedHeaderCounter > lstHeader.Count - 1)
                                    break;
                                string matchedHeader = lstHeader[matchedHeaderCounter];
                                if (header == matchedHeader)
                                {
                                    matchedHeaderCounter++;
                                    ParameterValue log = new ParameterValue
                                    {
                                        Timestamp = dateStamp,
                                        DeviceId = deviceId,
                                        ParameterId = Convert.ToInt32(matchedHeader),
                                        Value = field
                                    };
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

        public List<Event> ReadEventLog(int deviceId, string eventType, string csvFilePath)
        {
            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null
            });

            return eventType switch
            {
                nameof(EventType.dip) => MapEvents<DipEvent>(csv, deviceId, eventType, (src, dst) => dst.Value += $", Min_Voltage: {src.Min_Voltage}"),
                nameof(EventType.interrupt) => MapEvents<InterruptEvent>(csv, deviceId, eventType),
                nameof(EventType.rvc) => MapEvents<RVCEvent>(csv, deviceId, eventType, (src, dst) => dst.Value += $", UMAX: {src.UMAX}, USS: {src.USS}"),
                nameof(EventType.swell) => MapEvents<SwellEvent>(csv, deviceId, eventType, (src, dst) => dst.Value += $", Max_Voltage: {src.Max_Voltage}"),
                nameof(EventType.shortflicker) or nameof(EventType.longflicker) => MapEvents<FlickerEvent>(csv, deviceId, eventType, (src, dst) => dst.Value += $", Date: {src.Date}, A: {src.A}, B: {src.B}, C: {src.C}"),
                _ => new List<Event>()
            };
        }

        private List<Event> MapEvents<T>(CsvReader csv, int deviceId, string eventType, Action<T, Event>? extraMapping = null)
            where T : IBaseEvent
        {
            var list = new List<Event>();
            int parameterId = eventType.Contains("flicker") ? 31 : 32;

            foreach (var item in csv.GetRecords<T>())
            {
                var ev = new Event
                {
                    DeviceId = deviceId,
                    ParameterId = parameterId,
                    Timestamp = item.Start_Time ?? DateTime.UtcNow,
                    Value = $"Type: {eventType}, Phase: {item.Phase}, Duration: {item.Duration}, Start: {item.Start_Time}, End: {item.End_Time}"
                };

                extraMapping?.Invoke(item, ev);
                list.Add(ev);
            }

            return list;
        }
    }
}
