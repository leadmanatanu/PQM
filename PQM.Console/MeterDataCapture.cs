using System.Text;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Microsoft.Extensions.Configuration;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;

namespace PQM.Console;

internal static class MeterDataCapture
{
    public static void Run(IConfiguration config)
    {
        string connectionString = config.GetSection("ConnectionString").Value ?? string.Empty;
        int clientAddress = config.GetValue<int>("DlmsSettings:ClientAddress", 1);
        int serverAddress = config.GetValue<int>("DlmsSettings:ServerAddress", 1);
        string authStr = config.GetValue<string>("DlmsSettings:Authentication", "None");
        string password = config.GetValue<string>("DlmsSettings:Password", "");
        bool useLogicalNameReferencing = config.GetValue<bool>("DlmsSettings:UseLogicalNameReferencing", true);
        string standardStr = config.GetValue<string>("DlmsSettings:Standard", "DLMS");

        Enum.TryParse<Authentication>(authStr, true, out var authentication);
        if (!Enum.TryParse<Standard>(standardStr, true, out var standard))
        {
            standard = Standard.DLMS;
        }

        var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "meter data.txt"));
        var report = new StringBuilder();
        report.AppendLine("Meter Data Capture");
        report.AppendLine($"Captured at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"DLMS settings: Client={clientAddress}, Server={serverAddress}, Authentication={authentication}, Standard={standard}, LN={useLogicalNameReferencing}");
        report.AppendLine();

        List<Device> devices;
        using (var db = new DataContext(connectionString))
        {
            devices = db.Device
                .Where(device => device.IsActive && !device.IsDeleted)
                .OrderBy(device => device.Id)
                .ToList();
        }

        if (devices.Count == 0)
        {
            report.AppendLine("No active devices found in the database.");
        }

        foreach (var device in devices)
        {
            report.AppendLine(new string('=', 100));
            report.AppendLine($"Device: {device.Name} (ID {device.Id})");
            report.AppendLine($"Endpoint: {device.IP}:{device.PORT}");

            if (string.IsNullOrWhiteSpace(device.IP) || device.PORT <= 0)
            {
                report.AppendLine("Status: Skipped. Device IP/port is missing.");
                report.AppendLine();
                continue;
            }

            try
            {
                using var reader = new DLMSReader(
                    device.IP,
                    device.PORT,
                    clientAddress,
                    serverAddress,
                    authentication,
                    password,
                    useLogicalNameReferencing,
                    standard);

                reader.Connect();
                var converter = new GXDLMSConverter();
                try
                {
                    converter.UpdateOBISCodeInformation(reader.Objects);
                }
                catch
                {
                    // Descriptions are helpful, but the raw object list is still valid without them.
                }

                report.AppendLine("Status: Connected");
                report.AppendLine($"Association objects from meter: {reader.Objects.Count}");
                report.AppendLine();
                report.AppendLine("Object type counts:");
                foreach (var group in reader.Objects.GroupBy(obj => obj.ObjectType).OrderBy(group => group.Key.ToString()))
                {
                    report.AppendLine($"- {group.Key}: {group.Count()}");
                }

                report.AppendLine();
                report.AppendLine("Objects and values read from meter:");

                foreach (GXDLMSObject obj in reader.Objects.OrderBy(obj => obj.ObjectType.ToString()).ThenBy(obj => obj.LogicalName))
                {
                    string description = string.IsNullOrWhiteSpace(obj.Description)
                        ? $"{obj.ObjectType} {obj.LogicalName}"
                        : obj.Description;

                    report.AppendLine();
                    report.AppendLine($"[{obj.ObjectType}] {obj.LogicalName} - {description}");
                    report.AppendLine($"Version: {obj.Version}");

                    if (obj is IGXDLMSBase dlmsBase)
                    {
                        var attributes = dlmsBase.GetAttributeIndexToRead(false);
                        report.AppendLine($"Attributes suggested by Gurux for read: {FormatIndexes(attributes)}");
                    }

                    AppendAccessRows(report, obj);
                    AppendValue(report, "Attribute 2 / primary value", () => reader.ReadObjectValue(obj));

                    if (obj.ObjectType == ObjectType.Register ||
                        obj.ObjectType == ObjectType.ExtendedRegister ||
                        obj.ObjectType == ObjectType.DemandRegister)
                    {
                        AppendValue(report, "Attribute 3 / scaler-unit", () => reader.ReadObjectAttribute3(obj));
                    }

                    if (obj.ObjectType == ObjectType.AssociationLogicalName)
                    {
                        AppendValue(report, "Attribute 4 / application context", () => reader.ReadObjectAttribute(obj, 4));
                        AppendValue(report, "Attribute 6 / authentication mechanism", () => reader.ReadObjectAttribute(obj, 6));
                        AppendValue(report, "Attribute 8 / association status", () => reader.ReadObjectAttribute(obj, 8));
                    }

                    if (IsEventStatusObject(obj.LogicalName))
                    {
                        report.AppendLine("Event section: Standard event-status OBIS object. Active state is decoded from the device value above.");
                    }
                }
            }
            catch (Exception ex)
            {
                report.AppendLine("Status: Connection/read failed");
                report.AppendLine($"Error: {ex.Message}");
            }

            report.AppendLine();
        }

        File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);
        System.Console.WriteLine($"Meter data written to: {outputPath}");
    }

    private static void AppendAccessRows(StringBuilder report, GXDLMSObject obj)
    {
        if (obj.Attributes?.Count > 0)
        {
            report.AppendLine("Attribute access from meter association:");
            foreach (var attr in obj.Attributes.OrderBy(attr => attr.Index))
            {
                report.AppendLine($"  - Attribute {attr.Index}: {attr.Access}");
            }
        }

        if (obj.MethodAttributes?.Count > 0)
        {
            report.AppendLine("Method access from meter association:");
            foreach (var method in obj.MethodAttributes.OrderBy(method => method.Index))
            {
                report.AppendLine($"  - Method {method.Index}: {method.MethodAccess}");
            }
        }
    }

    private static void AppendValue(StringBuilder report, string label, Func<string> read)
    {
        try
        {
            string value = read();
            string source = value.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ? "Device error" : "Device";
            report.AppendLine($"{label}: [{source}] {TrimLong(value)}");
        }
        catch (Exception ex)
        {
            report.AppendLine($"{label}: [Device error] {ex.Message}");
        }
    }

    private static string FormatIndexes(int[] indexes)
    {
        return indexes.Length == 0 ? "(none)" : string.Join(", ", indexes);
    }

    private static string TrimLong(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        value = value.Replace("\r", " ").Replace("\n", " ");
        return value.Length <= 1200 ? value : value[..1200] + "...";
    }

    private static bool IsEventStatusObject(string logicalName)
    {
        return logicalName is
            "0.0.96.11.0.255" or
            "0.0.96.11.1.255" or
            "0.0.96.11.2.255" or
            "0.0.96.11.3.255" or
            "0.0.96.11.4.255";
    }
}
