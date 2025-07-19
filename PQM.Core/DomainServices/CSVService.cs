using CsvHelper;
using Microsoft.VisualBasic.FileIO;
using PQM.Core.Entities;
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
                    Console.WriteLine("Row:");
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
    }
}
