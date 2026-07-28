using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualBasic.FileIO;
using PQM.Core.Entities;
using PQM.Core.Helpers;
using PQM.Core.IRepositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PQM.Infrastructure.Services
{
    public class CSVService : ICSVService
    {
        public List<ReadingValue> ReadCSVData(int deviceId, string csvFilePath, List<string> mappedParameter)
        {
            List<ReadingValue> logList = new List<ReadingValue>();
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
                            if (mappedParameter.Any(x => x.ToLower() == field.ToLower()))
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
                                string headerName = lstAllHeader[innerCounter];
                                if (mappedParameter.Any(x => x.ToLower() == headerName.ToLower()))
                                {
                                    logList.Add(new ReadingValue
                                    {
                                        ParameterId = matchedHeaderCounter + 1,
                                        Value = field
                                    });
                                    matchedHeaderCounter++;
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

        public List<DeviceEvent> ReadEventLog(int deviceId, string eventType, string csvFilePath)
        {
            var list = new List<DeviceEvent>();
            if (!File.Exists(csvFilePath)) return list;

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null
            });

            int parameterId = eventType.Contains("flicker") ? 31 : 32;

            try
            {
                while (csv.Read())
                {
                    var ev = new DeviceEvent
                    {
                        DeviceId = deviceId,
                        ParameterId = parameterId,
                        EventTime = DateTime.UtcNow,
                        RawValue = $"Type: {eventType}"
                    };
                    list.Add(ev);
                }
            }
            catch { }

            return list;
        }
    }
}
