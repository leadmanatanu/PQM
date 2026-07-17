using Microsoft.Extensions.Configuration;
using PQM.Core.Entities;
using PQM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace PQM.ConsoleApp
{
    using Console = System.Console;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("     PQM DLMS Background Reader Console Service   ");
            Console.WriteLine("==================================================");

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string connectionString = config["ConnectionString"] ?? string.Empty;
            bool useDatabase = config.GetValue<bool>("UseDatabase", true);
            int syncIntervalMinutes = config.GetValue<int>("SyncIntervalMinutes", 15);

            int clientAddress = config.GetValue<int>("DlmsSettings:ClientAddress", 32);
            int serverAddress = config.GetValue<int>("DlmsSettings:ServerAddress", 1);
            string authStr = config.GetValue<string>("DlmsSettings:Authentication", "None");
            string dlmsPassword = config.GetValue<string>("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = config.GetValue<bool>("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = config.GetValue<string>("DlmsSettings:Standard", "DLMS");

            Enum.TryParse<Authentication>(authStr, true, out var authentication);
            Enum.TryParse<Standard>(standardStr, true, out var standard);

            // Fetch device list
            List<Device> devices = new List<Device>();

            if (useDatabase)
            {
                using (var db = new DataContext(connectionString))
                {
                    devices = db.Device.Where(d => d.IsActive && !d.IsDeleted).ToList();
                }
                Console.WriteLine($"[Service] Loaded {devices.Count} active devices from database.");
            }
            else
            {
                string deviceIP = config["DlmsDevice:IP"] ?? "127.0.0.1";
                int devicePort = config.GetValue<int>("DlmsDevice:PORT", 4005);
                string deviceName = config["DlmsDevice:Name"] ?? "Test DLMS Meter";

                devices.Add(new Device
                {
                    Id = 1,
                    Name = deviceName,
                    IP = deviceIP,
                    PORT = devicePort,
                    IsActive = true
                });
                Console.WriteLine($"[Service] Running in Database-Free mode for {deviceName} at {deviceIP}:{devicePort}.");
            }

            if (devices.Count == 0)
            {
                Console.WriteLine("[Service] No active devices configured. Exiting.");
                return;
            }

            Console.WriteLine("\nStarting continuous DLMS reading loop.");
            Console.WriteLine("Press ESC or 'q' to stop reading and exit...");

            bool keepRunning = true;
            while (keepRunning)
            {
                foreach (var device in devices)
                {
                    if (string.IsNullOrEmpty(device.IP) || device.PORT == 0)
                        continue;

                    Console.WriteLine($"\n--------------------------------------------------");
                    Console.WriteLine($"Connecting to {device.Name} at {device.IP}:{device.PORT}...");
                    
                    try
                    {
                        using (var reader = new DLMSReader(
                            device.IP, 
                            device.PORT, 
                            clientAddress, 
                            serverAddress, 
                            authentication, 
                            dlmsPassword, 
                            useLogicalNameReferencing, 
                            standard))
                        {
                            reader.Connect();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Connected to {device.Name} successfully!");
                            Console.ResetColor();

                            // Retrieve Association view
                            reader.GetAssociationView();

                            // Fetch parameters to read from DB (or use local fallback)
                            List<Parameter> parameters;
                            if (useDatabase)
                            {
                                using (var db = new DataContext(connectionString))
                                {
                                    parameters = db.Parameter.Where(p => p.IsActive).ToList();
                                }
                            }
                            else
                            {
                                // Local fallback matching user's list
                                parameters = GetFallbackParameters();
                            }

                            var parameterValuesToSave = new List<ParameterValue>();
                            var eventsToSave = new List<Event>();

                            foreach (var param in parameters)
                            {
                                // Find object in meter association
                                GXDLMSObject? obj = null;
                                if (reader.Objects != null)
                                {
                                    foreach (var o in reader.Objects)
                                    {
                                        if (o.LogicalName == param.ObisCode)
                                        {
                                            obj = o;
                                            break;
                                        }
                                    }
                                }

                                if (obj == null)
                                {
                                    obj = GXDLMSClient.CreateObject(ObjectType.Register);
                                    obj.LogicalName = param.ObisCode;
                                }

                                string readVal = reader.ReadObjectValue(obj);
                                if (!string.IsNullOrEmpty(readVal) && !readVal.StartsWith("Error"))
                                {
                                    Console.WriteLine($" - {param.Name} ({param.ObisCode}): {readVal}");

                                    if (param.ObisCode.StartsWith("0.0.96.11."))
                                    {
                                        eventsToSave.Add(new Event
                                        {
                                            DeviceId = device.Id,
                                            ParameterId = param.Id,
                                            Value = readVal,
                                            Timestamp = DateTime.UtcNow
                                        });
                                    }
                                    else
                                    {
                                        parameterValuesToSave.Add(new ParameterValue
                                        {
                                            DeviceId = device.Id,
                                            ParameterId = param.Id,
                                            Value = readVal,
                                            Timestamp = DateTime.UtcNow
                                        });
                                    }
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($" - {param.Name} ({param.ObisCode}): Read failed ({readVal})");
                                    Console.ResetColor();
                                }
                            }

                            // Save to database
                            if (useDatabase && (parameterValuesToSave.Count > 0 || eventsToSave.Count > 0))
                            {
                                using (var db = new DataContext(connectionString))
                                {
                                    if (parameterValuesToSave.Count > 0)
                                        db.ParameterValue.AddRange(parameterValuesToSave);
                                    if (eventsToSave.Count > 0)
                                        db.Event.AddRange(eventsToSave);

                                    // Update LastSync on device
                                    var dev = db.Device.FirstOrDefault(d => d.Id == device.Id);
                                    if (dev != null)
                                        dev.LastSync = DateTime.UtcNow;

                                    db.SaveChanges();
                                }
                                Console.WriteLine($"[Database] Successfully saved {parameterValuesToSave.Count} readings and {eventsToSave.Count} events.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Error] Failed reading device {device.Name}: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                int sleepIntervalMs = syncIntervalMinutes * 60 * 1000;
                Console.WriteLine($"\nWaiting {syncIntervalMinutes} minutes before next sync run...");
                // Check key input
                int slept = 0;
                while (slept < sleepIntervalMs)
                {
                    try
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q' || key.KeyChar == 'Q')
                            {
                                keepRunning = false;
                                break;
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        Thread.Sleep(sleepIntervalMs - slept);
                        break;
                    }
                    Thread.Sleep(100);
                    slept += 100;
                }
            }

            Console.WriteLine("Exiting Background Reader Service.");
        }

        private static List<Parameter> GetFallbackParameters()
        {
            return new List<Parameter>
            {
                new Parameter { Id = 1, Name = "Accuracy Test Start", ObisCode = "0.128.162.0.128.255" },
                new Parameter { Id = 2, Name = "Accuracy Test Stop", ObisCode = "0.128.162.1.128.255" },
                new Parameter { Id = 3, Name = "Activity Calendar", ObisCode = "0.0.13.0.0.255" },
                new Parameter { Id = 4, Name = "Apparent Power – kVA", ObisCode = "1.0.9.7.0.255" },
                new Parameter { Id = 5, Name = "Association LN Meter Reader", ObisCode = "0.0.40.0.2.255" },
                new Parameter { Id = 6, Name = "Available Billing Periods", ObisCode = "0.0.0.1.1.255" },
                new Parameter { Id = 7, Name = "Billing Date", ObisCode = "0.0.0.1.2.255" },
                new Parameter { Id = 8, Name = "Billing Period Script Table", ObisCode = "0.0.10.0.1.255" },
                new Parameter { Id = 9, Name = "Capture Period of Daily Load Profile", ObisCode = "1.0.0.8.5.255" },
                new Parameter { Id = 10, Name = "Category", ObisCode = "0.0.94.91.11.255" },
                new Parameter { Id = 11, Name = "CMRI Reset", ObisCode = "0.128.154.128.128.255" },
                new Parameter { Id = 12, Name = "CT Rating", ObisCode = "0.0.94.91.12.255" },
                new Parameter { Id = 13, Name = "Cumulative Billing Count", ObisCode = "0.0.0.1.0.255" },
                new Parameter { Id = 14, Name = "Cumulative Energy – kVAh (Export)", ObisCode = "1.0.10.8.0.255" },
                new Parameter { Id = 15, Name = "Cumulative Energy (kVAh)", ObisCode = "1.0.9.8.0.255" },
                new Parameter { Id = 16, Name = "Cumulative Energy (kvarh) – Lag", ObisCode = "1.0.5.8.0.255" },
                new Parameter { Id = 17, Name = "Cumulative Energy (kvarh) – Lead", ObisCode = "1.0.8.8.0.255" },
                new Parameter { Id = 18, Name = "Cumulative Energy (kWh)", ObisCode = "1.0.1.8.0.255" },
                new Parameter { Id = 19, Name = "Cumulative Energy (kWh) – Export", ObisCode = "1.0.2.8.0.255" },
                new Parameter { Id = 20, Name = "Cumulative Power Failure Duration", ObisCode = "0.0.94.91.8.255" },
                new Parameter { Id = 21, Name = "Cumulative Programming Count", ObisCode = "0.0.96.2.0.255" },
                new Parameter { Id = 22, Name = "Cumulative Tamper Count", ObisCode = "0.0.94.91.0.255" },
                new Parameter { Id = 23, Name = "Current – IB", ObisCode = "1.0.71.7.0.255" },
                new Parameter { Id = 24, Name = "Current – IR", ObisCode = "1.0.31.7.0.255" },
                new Parameter { Id = 25, Name = "Current – IY", ObisCode = "1.0.51.7.0.255" },
                new Parameter { Id = 26, Name = "Current Related Event Code", ObisCode = "0.0.96.11.1.255" },
                new Parameter { Id = 27, Name = "Power Failure Related Event Code", ObisCode = "0.0.96.11.2.255" },
                new Parameter { Id = 28, Name = "Profile Capture Period", ObisCode = "1.0.0.8.4.255" },
                new Parameter { Id = 29, Name = "PT Power Fail Tamper Events", ObisCode = "1.0.128.7.90.255" },
                new Parameter { Id = 30, Name = "Reset Type", ObisCode = "0.128.153.128.128.255" },
                new Parameter { Id = 31, Name = "Signed Active Power – kW", ObisCode = "1.0.1.7.0.255" },
                new Parameter { Id = 32, Name = "Signed Power Factor – B Phase", ObisCode = "1.0.73.7.0.255" },
                new Parameter { Id = 33, Name = "Signed Power Factor – R Phase", ObisCode = "1.0.33.7.0.255" },
                new Parameter { Id = 34, Name = "Signed Power Factor – Y Phase", ObisCode = "1.0.53.7.0.255" },
                new Parameter { Id = 35, Name = "Signed Reactive Power – kvar", ObisCode = "1.0.3.7.0.255" },
                new Parameter { Id = 36, Name = "Single Action Schedule for Billing Dates", ObisCode = "0.0.15.0.0.255" },
                new Parameter { Id = 37, Name = "TCP/UDP Setup", ObisCode = "0.0.25.0.0.255" },
                new Parameter { Id = 38, Name = "TCP/UDP Setup IPv4 Address", ObisCode = "0.0.25.1.0.255" },
                new Parameter { Id = 39, Name = "TCP/UDP Setup MAC Address", ObisCode = "0.0.25.2.0.255" },
                new Parameter { Id = 40, Name = "Transaction Related Event Code", ObisCode = "0.0.96.11.3.255" },
                new Parameter { Id = 41, Name = "Voltage – VBN", ObisCode = "1.0.72.7.0.255" },
                new Parameter { Id = 42, Name = "Voltage – VRN", ObisCode = "1.0.32.7.0.255" },
                new Parameter { Id = 43, Name = "Voltage – VYN", ObisCode = "1.0.52.7.0.255" }
            };
        }
    }
}
