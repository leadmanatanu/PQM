using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.Net;
using Gurux.Common;
using PQM.Core.Entities;

namespace PQM.Infrastructure.Services
{
    public class DiscoveredParameter
    {
        public required string Name { get; set; }
        public required string ObisCode { get; set; }
        public required string ObjectType { get; set; }
        public required string Value { get; set; }
    }

    public class DLMSReader : IDisposable
    {
        private readonly GXNet _media;
        private readonly GXDLMSClient _client;
        private bool _isConnected = false;

        public int WaitTime { get; set; } = 5000; // ms
        public int RetryCount { get; set; } = 3;

        public GXDLMSObjectCollection Objects => _client.Objects;

        public DLMSReader(string ipAddress, int port, int clientAddress = 16, int serverAddress = 1, Authentication authentication = Authentication.None, string password = "", bool useLogicalNameReferencing = true, Standard standard = Standard.DLMS, InterfaceType interfaceType = InterfaceType.WRAPPER)
        {
            _media = new GXNet(NetworkType.Tcp, ipAddress, port);
            
            _client = new GXDLMSClient(useLogicalNameReferencing)
            {
                InterfaceType = interfaceType,
                ClientAddress = clientAddress,
                ServerAddress = serverAddress,
                Authentication = authentication,
                Password = string.IsNullOrEmpty(password) ? null : System.Text.Encoding.ASCII.GetBytes(password),
                Standard = standard
            };

            if (!useLogicalNameReferencing)
            {
                _client.ProposedConformance = Conformance.Read | Conformance.Write | Conformance.SelectiveAccess;
            }
        }

        public void Connect()
        {
            _media.Open();

            // Establish DLMS connection (Association)
            var reply = new GXReplyData();
            byte[][] aarq = _client.AARQRequest();
            if (aarq != null)
            {
                if (ReadDataBlock(aarq, reply))
                {
                    if (reply.Data != null)
                    {
                        _client.ParseAAREResponse(reply.Data);
                        _isConnected = true;
                    }
                }
                else
                {
                    throw new Exception($"Association failed with error code: {reply.Error}");
                }
            }

            // Retrieve the association view to populate _client.Objects with correct types and class IDs
            try
            {
                GetAssociationView();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DLMS Reader] Warning: Failed to retrieve association view: {ex.Message}");
                Console.ResetColor();
            }
        }

        public string ReadRegister(string obisCode, string parameterName = "")
        {
            try
            {
                GXDLMSObject? obj = null;

                // Try to find the object in the association view by Logical Name (OBIS) only
                if (_client.Objects != null)
                {
                    foreach (var o in _client.Objects)
                    {
                        if (o.LogicalName == obisCode)
                        {
                            obj = o;
                            break;
                        }
                    }
                }

                // If not found in association view, fall back to creating it manually
                if (obj == null)
                {
                    var objectType = ObjectType.Register;
                    if (!string.IsNullOrEmpty(parameterName) && parameterName.Contains(" - "))
                    {
                        var typePart = parameterName.Split(new[] { " - " }, StringSplitOptions.None)[0];
                        Enum.TryParse<ObjectType>(typePart, out objectType);
                    }

                    obj = GXDLMSClient.CreateObject(objectType);
                    obj.LogicalName = obisCode;
                }

                return ReadObjectValue(obj);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public void WriteRegister(string obisCode, string stringValue, int attributeIndex = 2)
        {
            if (_client.Objects == null)
            {
                throw new InvalidOperationException("Association view is not loaded. Call Connect() first.");
            }

            GXDLMSObject? obj = null;
            foreach (var o in _client.Objects)
            {
                if (o.LogicalName == obisCode)
                {
                    obj = o;
                    break;
                }
            }

            if (obj == null)
            {
                throw new Exception($"Object with OBIS code {obisCode} not found in the meter.");
            }

            // Parse the string value to the correct type
            object newValue = stringValue;
            if (int.TryParse(stringValue, out int intVal))
            {
                newValue = intVal;
            }
            else if (double.TryParse(stringValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
            {
                newValue = doubleVal;
            }
            else if (bool.TryParse(stringValue, out bool boolVal))
            {
                newValue = boolVal;
            }
            else if (DateTime.TryParse(stringValue, out DateTime dtVal))
            {
                newValue = dtVal;
            }

            // Set the value on the object
            if (obj is GXDLMSRegister reg)
            {
                reg.Value = newValue;
            }
            else if (obj is GXDLMSData data)
            {
                data.Value = newValue;
            }
            else
            {
                _client.UpdateValue(obj, attributeIndex, newValue);
            }

            // Generate write request packets
            byte[][] writeReq = _client.Write(obj, attributeIndex);
            var reply = new GXReplyData();
            if (writeReq != null)
            {
                if (!ReadDataBlock(writeReq, reply))
                {
                    throw new Exception($"Write failed. DLMS Error code: {reply.Error}");
                }
            }
        }

        private string FormatValue(object? val)
        {
            if (val == null) return "";
            if (val is Array arr && val is not byte[])
            {
                var parts = new List<string>();
                foreach (var item in arr) parts.Add(FormatValue(item));
                return string.Join(", ", parts);
            }
            if (val is byte[] bytes)
            {
                // DLMS DateTime is always exactly 12 bytes: year(2)+month+day+dow+hour+min+sec+hundredths+deviation(2)+status
                // Try to parse it into a readable date before falling back to hex.
                if (bytes.Length == 12)
                {
                    try
                    {
                        // DLMS Clock format: Year(2) Month Day DoW Hour Min Sec Hundredths Deviation(2) Status
                        int year   = (bytes[0] << 8) | bytes[1];  // big-endian
                        int month  = bytes[2];
                        int day    = bytes[3];
                        // bytes[4] = day-of-week (skip)
                        int hour   = bytes[5];
                        int minute = bytes[6];
                        int second = bytes[7];
                        // 0xFF means wildcard / not specified
                        if (year >= 1 && year <= 9999 && month >= 1 && month <= 12 && day >= 1 && day <= 31
                            && hour <= 23 && minute <= 59 && second <= 59)
                        {
                            int sec = (second == 0xFF) ? 0 : second;
                            return $"{year:D4}-{month:D2}-{day:D2} {hour:D2}:{minute:D2}:{sec:D2}";
                        }
                    }
                    catch { /* not a valid DateTime — fall through to hex */ }
                }
                return BitConverter.ToString(bytes).Replace("-", " ");
            }
            // Handle nested arrays (e.g. sub-structures in billing/load profiles)
            if (val is object[] objArr)
            {
                return string.Join(", ", objArr.Select(v => FormatValue(v)));
            }
            if (val is System.Collections.IEnumerable enumerable && val is not string)
            {
                var parts = new List<string>();
                foreach (var item in enumerable) parts.Add(FormatValue(item));
                return string.Join(", ", parts);
            }
            // GXDateTime returned directly (not as byte[])
            if (val is Gurux.DLMS.GXDateTime gxdtDirect)
            {
                try
                {
                    var d = gxdtDirect.Value.DateTime;
                    return $"{d.Year:D4}-{d.Month:D2}-{d.Day:D2} {d.Hour:D2}:{d.Minute:D2}:{d.Second:D2}";
                }
                catch { }
            }
            return val.ToString() ?? "";
        }

        private List<string> GetCaptureObjectColumns(GXDLMSProfileGeneric profile)
        {
            var cols = new List<string>();
            if (profile.CaptureObjects == null) return cols;
            var converter = new GXDLMSConverter();
            foreach (var co in profile.CaptureObjects)
            {
                try { converter.UpdateOBISCodeInformation(co.Key); } catch { }
                string name;
                if (!string.IsNullOrEmpty(co.Key.Description))
                {
                    name = co.Key.Description;
                }
                else
                {
                    // Fall back to a meaningful name: OBIS code + attribute index
                    string logicalName = co.Key.LogicalName ?? "Unknown";
                    int attrIndex = co.Value?.AttributeIndex ?? 2;
                    name = $"{logicalName} (Attr {attrIndex})";
                }
                cols.Add(name);
            }
            // Make column names unique
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < cols.Count; i++)
            {
                string c = cols[i];
                if (seen.ContainsKey(c)) { seen[c]++; cols[i] = $"{c} ({seen[c]})"; }
                else { seen[c] = 0; }
            }
            return cols;
        }

        private string ReadProfileGenericCaptureObjects(GXDLMSProfileGeneric profile)
        {
            try
            {
                byte[][] capCmd = _client.Read(profile, 3);
                var capReply = new GXReplyData();
                if (ReadDataBlock(capCmd, capReply))
                    _client.UpdateValue(profile, 3, capReply.Value);
                var cols = GetCaptureObjectColumns(profile);
                return string.Join(", ", cols);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private string ReadProfileGenericBuffer(GXDLMSProfileGeneric profile, DateTime? lastTimestamp = null)
        {
            // Must read CaptureObjects (attr 3) before Buffer (attr 2)
            try
            {
                byte[][] capCmd = _client.Read(profile, 3);
                var capReply = new GXReplyData();
                if (ReadDataBlock(capCmd, capReply))
                    _client.UpdateValue(profile, 3, capReply.Value);
            }
            catch { }

            try
            {
                byte[][] bufCmd;
                if (lastTimestamp.HasValue)
                {
                    bufCmd = _client.ReadRowsByRange(profile, lastTimestamp.Value, DateTime.UtcNow);
                }
                else
                {
                    bufCmd = _client.Read(profile, 2);
                }

                var bufReply = new GXReplyData();
                if (ReadDataBlock(bufCmd, bufReply))
                {
                    _client.UpdateValue(profile, 2, bufReply.Value);
                }
                else
                {
                    if (lastTimestamp.HasValue)
                        throw new Exception("Selective access read block failed.");
                    return "[]";
                }
            }
            catch (Exception ex)
            {
                if (lastTimestamp.HasValue)
                {
                    Console.WriteLine($"[DLMS Reader] Selective access failed: {ex.Message}. Falling back to full read...");
                    try
                    {
                        byte[][] bufCmd = _client.Read(profile, 2);
                        var bufReply = new GXReplyData();
                        if (ReadDataBlock(bufCmd, bufReply))
                        {
                            _client.UpdateValue(profile, 2, bufReply.Value);
                        }
                        else
                        {
                            return "[]";
                        }
                    }
                    catch
                    {
                        return "[]";
                    }
                }
                else
                {
                    return $"Error: {ex.Message}";
                }
            }

            if (profile.Buffer == null || profile.CaptureObjects == null || profile.CaptureObjects.Count == 0)
                return "[]";

            var colNames = GetCaptureObjectColumns(profile);
            var rows = new List<object>();
            foreach (var row in profile.Buffer)
            {
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < row.Length && i < colNames.Count; i++)
                    dict[colNames[i]] = FormatValue(row[i]);
                rows.Add(dict);
            }
            return System.Text.Json.JsonSerializer.Serialize(rows);
        }

        public static string GetFriendlyClassName(ObjectType type)
        {
            return type switch
            {
                ObjectType.ScriptTable => "Script Table",
                ObjectType.ActivityCalendar => "ActivityCalendar",
                ObjectType.ActionSchedule => "ActionSchedule",
                ObjectType.IecHdlcSetup => "IecHdlcSetup",
                ObjectType.TcpUdpSetup => "TcpUdpSetup",
                ObjectType.Ip4Setup => "Ip4Setup",
                ObjectType.MacAddressSetup => "MacAddressSetup",
                ObjectType.AssociationLogicalName => "AssociationLogicalName",
                ObjectType.ProfileGeneric => "ProfileGeneric",
                _ => type.ToString()
            };
        }

        private string FormatActionScheduleScript(GXDLMSActionSchedule schedule)
        {
            if (schedule.Target == null) return "(none)";
            string name = schedule.Target.LogicalName ?? "";
            if (!string.IsNullOrEmpty(schedule.Target.Description))
                name += " " + schedule.Target.Description;
            return $"{name} (Selector: {schedule.ExecutedScriptSelector})";
        }

        private static string FormatSeasonProfiles(GXDLMSSeasonProfile[]? profiles)
        {
            if (profiles == null || profiles.Length == 0) return "None";
            return string.Join("; ", profiles.Select(p =>
            {
                string name = p.Name != null ? System.Text.Encoding.ASCII.GetString(p.Name) : "";
                string week = p.WeekName != null ? System.Text.Encoding.ASCII.GetString(p.WeekName) : "";
                return $"{name}: Start={p.Start}, Week={week}";
            }));
        }

        private static string FormatWeekProfiles(GXDLMSWeekProfile[]? profiles)
        {
            if (profiles == null || profiles.Length == 0) return "None";
            return string.Join("; ", profiles.Select(p =>
            {
                string name = p.Name != null ? System.Text.Encoding.ASCII.GetString(p.Name) : "";
                return $"{name}: {p.Monday},{p.Tuesday},{p.Wednesday},{p.Thursday},{p.Friday},{p.Saturday},{p.Sunday}";
            }));
        }

        private static string FormatDayProfiles(GXDLMSDayProfile[]? profiles)
        {
            if (profiles == null || profiles.Length == 0) return "None";
            return string.Join("; ", profiles.Select(p =>
            {
                string schedStr = "";
                if (p.DaySchedules != null && p.DaySchedules.Length > 0)
                    schedStr = string.Join(", ", p.DaySchedules.Select(s =>
                        $"{s.StartTime}: {s.ScriptLogicalName} #{s.ScriptSelector}"));
                return $"Day {p.DayId}: [{schedStr}]";
            }));
        }

        public string ReadObjectValue(GXDLMSObject obj, DateTime? lastTimestamp = null)
        {
            try
            {
                // If the object is a register type, read Attribute 3 (Scaler & Unit) first so the scaler is populated
                if (obj is GXDLMSRegister)
                {
                    try
                    {
                        byte[][] readScale = _client.Read(obj, 3);
                        var replyScale = new GXReplyData();
                        if (ReadDataBlock(readScale, replyScale))
                        {
                            _client.UpdateValue(obj, 3, replyScale.Value);
                        }
                    }
                    catch { }
                }

                byte[][] readCmd = _client.Read(obj, 2);
                var reply = new GXReplyData();

                if (ReadDataBlock(readCmd, reply))
                {
                    _client.UpdateValue(obj, 2, reply.Value);

                    if (obj is GXDLMSProfileGeneric pgProfile)
                    {
                        return ReadProfileGenericBuffer(pgProfile, lastTimestamp);
                    }

                    if (obj is GXDLMSClock clock)
                    {
                        return clock.Time?.ToString() ?? "";
                    }
                    
                    if (obj is GXDLMSScriptTable scriptTable)
                    {
                        var actionsList = new List<string>();
                        var converter = new GXDLMSConverter();
                        if (scriptTable.Scripts != null)
                        {
                            foreach (var script in scriptTable.Scripts)
                            {
                                if (script.Actions != null)
                                {
                                    foreach (var action in script.Actions)
                                    {
                                        string targetObis = action.Target?.LogicalName ?? "";
                                        if (action.Target != null)
                                        {
                                            try
                                            {
                                                converter.UpdateOBISCodeInformation(action.Target);
                                                if (!string.IsNullOrEmpty(action.Target.Description))
                                                {
                                                    targetObis += " " + action.Target.Description;
                                                }
                                            }
                                            catch { }
                                        }
                                        string actionStr = $"{script.Id}: {action.Type} {targetObis}";
                                        if (action.Index > 0)
                                        {
                                            actionStr += $" ({action.Index}";
                                            if (action.Parameter != null)
                                            {
                                                actionStr += $" = {action.Parameter}";
                                            }
                                            actionStr += ")";
                                        }
                                        actionsList.Add(actionStr);
                                    }
                                }
                            }
                        }
                        return string.Join("; ", actionsList);
                    }

                    if (obj is GXDLMSActionSchedule actionSchVal)
                    {
                        // Also read type and execution time for a fuller summary
                        try
                        {
                            byte[][] cmd3 = _client.Read(actionSchVal, 3);
                            var r3 = new GXReplyData();
                            if (ReadDataBlock(cmd3, r3)) _client.UpdateValue(actionSchVal, 3, r3.Value);
                        } catch { }
                        try
                        {
                            byte[][] cmd4 = _client.Read(actionSchVal, 4);
                            var r4 = new GXReplyData();
                            if (ReadDataBlock(cmd4, r4)) _client.UpdateValue(actionSchVal, 4, r4.Value);
                        } catch { }
                        string scriptStr = FormatActionScheduleScript(actionSchVal);
                        string typeStr = actionSchVal.Type.ToString();
                        string timeStr = actionSchVal.ExecutionTime != null
                            ? string.Join(", ", actionSchVal.ExecutionTime.Select(t => t.ToString()))
                            : "";
                        return $"Script: {scriptStr} | Type: {typeStr} | Time: {timeStr}";
                    }

                    if (obj is GXDLMSActivityCalendar actCalVal)
                    {
                        if (!string.IsNullOrEmpty(actCalVal.CalendarNameActive))
                            return actCalVal.CalendarNameActive;
                        return FormatValue(reply.Value);
                    }
                    
                    if (obj is GXDLMSRegister reg)
                    {
                        return FormatValue(reg.Value);
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc)
                    {
                        var decodedList = new List<object>();
                        foreach (var item in assoc.ObjectList)
                        {
                            var attrAccess = string.Join(", ", item.Attributes.Select(a => $"{a.Index} = {a.Access}"));
                            var methodAccess = string.Join(", ", item.MethodAttributes.Select(m => $"{m.Index} = {m.MethodAccess}"));
                            
                            decodedList.Add(new {
                                ClassId = GetFriendlyClassName(item.ObjectType),
                                Version = item.Version,
                                LogicalName = item.LogicalName,
                                AttributeAccess = attrAccess,
                                MethodAccess = methodAccess
                            });
                        }
                        return System.Text.Json.JsonSerializer.Serialize(decodedList);
                    }
                    
                    var valProp = obj.GetType().GetProperty("Value");
                    if (valProp != null)
                    {
                        return FormatValue(valProp.GetValue(obj));
                    }
                    
                    return FormatValue(reply.Value);
                }
                else
                {
                    return $"Error: {reply.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public string ReadObjectAttribute3(GXDLMSObject obj)
        {
            if (!(obj is GXDLMSRegister))
            {
                return "";
            }
            try
            {
                byte[][] readCmd = _client.Read(obj, 3);
                var reply = new GXReplyData();

                if (ReadDataBlock(readCmd, reply))
                {
                    _client.UpdateValue(obj, 3, reply.Value);
                    var reg = (GXDLMSRegister)obj;
                    return $"{{{reg.Scaler}, {reg.Unit}}}";
                }
                else
                {
                    return $"Error: {reply.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public string ReadObjectAttribute(GXDLMSObject obj, int attributeId, DateTime? lastTimestamp = null)
        {
            try
            {
                if (obj is GXDLMSRegister && attributeId == 2)
                {
                    try
                    {
                        byte[][] readScale = _client.Read(obj, 3);
                        var replyScale = new GXReplyData();
                        if (ReadDataBlock(readScale, replyScale))
                        {
                            _client.UpdateValue(obj, 3, replyScale.Value);
                        }
                    }
                    catch { }
                }

                if (obj is GXDLMSRegister && attributeId == 3)
                {
                    return ReadObjectAttribute3(obj);
                }

                byte[][] readCmd = _client.Read(obj, attributeId);
                var reply = new GXReplyData();

                if (ReadDataBlock(readCmd, reply))
                {
                    _client.UpdateValue(obj, attributeId, reply.Value);

                    if (obj is GXDLMSProfileGeneric pgAttr)
                    {
                        if (attributeId == 2)
                            return ReadProfileGenericBuffer(pgAttr, lastTimestamp);
                        if (attributeId == 3)
                            return ReadProfileGenericCaptureObjects(pgAttr);
                    }

                    if (obj is GXDLMSClock clock)
                    {
                        return attributeId switch
                        {
                            2 => clock.Time?.ToString() ?? "",
                            3 => clock.TimeZone.ToString(),
                            4 => clock.Status.ToString(),
                            5 => clock.Begin?.ToString() ?? "",
                            6 => clock.End?.ToString() ?? "",
                            7 => clock.Deviation.ToString(),
                            8 => clock.Enabled.ToString(),
                            9 => clock.ClockBase.ToString(),
                            _ => FormatValue(reply.Value)
                        };
                    }
                    
                    if (obj is GXDLMSScriptTable scriptTable && attributeId == 2)
                    {
                        var actionsList = new List<string>();
                        var converter = new GXDLMSConverter();
                        if (scriptTable.Scripts != null)
                        {
                            foreach (var script in scriptTable.Scripts)
                            {
                                if (script.Actions != null)
                                {
                                    foreach (var action in script.Actions)
                                    {
                                        string targetObis = action.Target?.LogicalName ?? "";
                                        if (action.Target != null)
                                        {
                                            try
                                            {
                                                converter.UpdateOBISCodeInformation(action.Target);
                                                if (!string.IsNullOrEmpty(action.Target.Description))
                                                {
                                                    targetObis += " " + action.Target.Description;
                                                }
                                            }
                                            catch { }
                                        }
                                        string actionStr = $"{script.Id}: {action.Type} {targetObis}";
                                        if (action.Index > 0)
                                        {
                                            actionStr += $" ({action.Index}";
                                            if (action.Parameter != null)
                                            {
                                                actionStr += $" = {action.Parameter}";
                                            }
                                            actionStr += ")";
                                        }
                                        actionsList.Add(actionStr);
                                    }
                                }
                            }
                        }
                        return string.Join("; ", actionsList);
                    }
                    
                    if (obj is GXDLMSRegister reg && attributeId == 2)
                    {
                        return FormatValue(reg.Value);
                    }

                    if (obj is GXDLMSExtendedRegister extReg)
                    {
                        if (attributeId == 4)
                            return extReg.Status != null ? extReg.Status.ToString() ?? "" : "";
                        if (attributeId == 5)
                            return extReg.CaptureTime.ToString();
                    }

                    if (obj is GXDLMSActionSchedule actionSchAttr)
                    {
                        return attributeId switch
                        {
                            2 => FormatActionScheduleScript(actionSchAttr),
                            3 => actionSchAttr.Type.ToString(),
                            4 => actionSchAttr.ExecutionTime != null
                                ? string.Join(", ", actionSchAttr.ExecutionTime.Select(t => t.ToString()))
                                : "",
                            _ => FormatValue(reply.Value)
                        };
                    }

                    if (obj is GXDLMSActivityCalendar actCalAttr)
                    {
                        return attributeId switch
                        {
                            2 => actCalAttr.CalendarNameActive ?? "",
                            3 => FormatSeasonProfiles(actCalAttr.SeasonProfileActive),
                            4 => FormatWeekProfiles(actCalAttr.WeekProfileTableActive),
                            5 => FormatDayProfiles(actCalAttr.DayProfileTableActive),
                            6 => actCalAttr.CalendarNamePassive ?? "",
                            7 => FormatSeasonProfiles(actCalAttr.SeasonProfilePassive),
                            8 => FormatWeekProfiles(actCalAttr.WeekProfileTablePassive),
                            9 => FormatDayProfiles(actCalAttr.DayProfileTablePassive),
                            10 => actCalAttr.Time != null ? actCalAttr.Time.ToString() : "",
                            _ => FormatValue(reply.Value)
                        };
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc && attributeId == 2)
                    {
                        var decodedList = new List<object>();
                        foreach (var item in assoc.ObjectList)
                        {
                            var attrAccess = string.Join(", ", item.Attributes.Select(a => $"{a.Index} = {a.Access}"));
                            var methodAccess = string.Join(", ", item.MethodAttributes.Select(m => $"{m.Index} = {m.MethodAccess}"));
                            
                            decodedList.Add(new {
                                ClassId = GetFriendlyClassName(item.ObjectType),
                                Version = item.Version,
                                LogicalName = item.LogicalName,
                                AttributeAccess = attrAccess,
                                MethodAccess = methodAccess
                            });
                        }
                        return System.Text.Json.JsonSerializer.Serialize(decodedList);
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc3 && attributeId == 3)
                    {
                        var data = new
                        {
                            ClientSAP = assoc3.ClientSAP,
                            ServerSAP = assoc3.ServerSAP
                        };
                        return System.Text.Json.JsonSerializer.Serialize(data);
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc4 && attributeId == 4)
                    {
                        var contextName = assoc4.ApplicationContextName;
                        if (contextName != null)
                        {
                            var data = new
                            {
                                RegistrationAuthority = contextName.CountryName == 756 ? "Switzerland" : contextName.CountryName.ToString(),
                                JointIsoCtt = contextName.JointIsoCtt,
                                Country = contextName.Country,
                                CountryName = contextName.CountryName,
                                IdentifiedOrganization = contextName.IdentifiedOrganization,
                                DlmsUA = contextName.DlmsUA,
                                ApplicationContext = contextName.ApplicationContext,
                                ContextId = contextName.ContextId.ToString()
                            };
                            return System.Text.Json.JsonSerializer.Serialize(data);
                        }
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc5 && attributeId == 5)
                    {
                        var contextInfo = assoc5.XDLMSContextInfo;
                        if (contextInfo != null)
                        {
                            var data = new
                            {
                                Conformance = contextInfo.Conformance.ToString(),
                                MaxReceivePduSize = contextInfo.MaxReceivePduSize,
                                MaxSendPduSize = contextInfo.MaxSendPduSize,
                                DlmsVersionNumber = contextInfo.DlmsVersionNumber
                            };
                            return System.Text.Json.JsonSerializer.Serialize(data);
                        }
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc6 && attributeId == 6)
                    {
                        var authName = assoc6.AuthenticationMechanismName;
                        if (authName != null)
                        {
                            var data = new
                            {
                                RegistrationAuthority = authName.CountryName == 756 ? "Switzerland" : authName.CountryName.ToString(),
                                JointIsoCtt = authName.JointIsoCtt,
                                Country = authName.Country,
                                CountryName = authName.CountryName,
                                IdentifiedOrganization = authName.IdentifiedOrganization,
                                DlmsUA = authName.DlmsUA,
                                AuthenticationMechanismName = authName.AuthenticationMechanismName,
                                MechanismId = authName.MechanismId.ToString()
                            };
                            return System.Text.Json.JsonSerializer.Serialize(data);
                        }
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc7 && attributeId == 7)
                    {
                        if (assoc7.Secret != null)
                        {
                            return System.Text.Encoding.ASCII.GetString(assoc7.Secret);
                        }
                        return "";
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc8 && attributeId == 8)
                    {
                        return assoc8.AssociationStatus.ToString();
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc9 && attributeId == 9)
                    {
                        return assoc9.SecuritySetupReference ?? "";
                    }

                    if (obj is GXDLMSAssociationLogicalName assoc10 && attributeId == 10)
                    {
                        if (assoc10.UserList != null)
                        {
                            var dict = new Dictionary<string, string>();
                            foreach (var u in assoc10.UserList)
                            {
                                dict[$"User {u.Key}"] = u.Value;
                            }
                            return System.Text.Json.JsonSerializer.Serialize(dict);
                        }
                        return "{}";
                    }
                    
                    var valProp = obj.GetType().GetProperty("Value");
                    if (valProp != null && attributeId == 2)
                    {
                        return FormatValue(valProp.GetValue(obj));
                    }
                    
                    return FormatValue(reply.Value);
                }
                else
                {
                    return $"Error: {reply.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public List<PQM.Core.Entities.Parameter> GetAssociationView()
        {
            var list = new List<PQM.Core.Entities.Parameter>();
            var reply = new GXReplyData();
            
            byte[][] request = _client.GetObjectsRequest();
            if (ReadDataBlock(request, reply))
            {
                _client.ParseObjects(reply.Data, true);
                var converter = new GXDLMSConverter();
                foreach (var obj in _client.Objects)
                {
                    // Filter for Registers only by default
                    if (obj.ObjectType != ObjectType.Register && 
                        obj.ObjectType != ObjectType.ExtendedRegister && 
                        obj.ObjectType != ObjectType.DemandRegister &&
                        obj.ObjectType != ObjectType.Data &&
                        obj.ObjectType != ObjectType.IecHdlcSetup &&
                        obj.ObjectType != ObjectType.TcpUdpSetup &&
                        obj.ObjectType != ObjectType.Ip4Setup &&
                        obj.ObjectType != ObjectType.MacAddressSetup &&
                        obj.ObjectType != ObjectType.AssociationLogicalName &&
                        obj.ObjectType != ObjectType.Clock &&
                        obj.ObjectType != ObjectType.ScriptTable &&
                        obj.ObjectType != ObjectType.ProfileGeneric &&
                        obj.ObjectType != ObjectType.ActionSchedule &&
                        obj.ObjectType != ObjectType.ActivityCalendar)
                    {
                        continue;
                    }

                    converter.UpdateOBISCodeInformation(obj);
                    string obis = obj.LogicalName;
                    string name = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obis}" : obj.Description;
                    
                    list.Add(new PQM.Core.Entities.Parameter
                    {
                        Name = name,
                        ObisCode = obis,
                        ObjectType = obj.ObjectType.ToString(),
                        IsActive = true,
                        IsDeleted = false,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }
            return list;
        }

        public List<DiscoveredParameter> GetAssociationViewWithValues(string? targetObjectType = null)
        {
            var list = new List<DiscoveredParameter>();
            var reply = new GXReplyData();
            
            byte[][] request = _client.GetObjectsRequest();
            if (ReadDataBlock(request, reply))
            {
                _client.ParseObjects(reply.Data, true);
                var converter = new GXDLMSConverter();
                foreach (var obj in _client.Objects)
                {
                    // Filter based on targetObjectType if provided (unless "All"), else default to Registers
                    if (!string.IsNullOrEmpty(targetObjectType) && !string.Equals(targetObjectType, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(obj.ObjectType.ToString(), targetObjectType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Default filter: Registers only
                        if (obj.ObjectType != ObjectType.Register && 
                            obj.ObjectType != ObjectType.ExtendedRegister && 
                            obj.ObjectType != ObjectType.DemandRegister &&
                            obj.ObjectType != ObjectType.Data &&
                            obj.ObjectType != ObjectType.IecHdlcSetup &&
                            obj.ObjectType != ObjectType.TcpUdpSetup &&
                            obj.ObjectType != ObjectType.Ip4Setup &&
                            obj.ObjectType != ObjectType.MacAddressSetup &&
                            obj.ObjectType != ObjectType.AssociationLogicalName &&
                            obj.ObjectType != ObjectType.Clock &&
                            obj.ObjectType != ObjectType.ScriptTable &&
                            obj.ObjectType != ObjectType.ProfileGeneric &&
                            obj.ObjectType != ObjectType.ActionSchedule &&
                            obj.ObjectType != ObjectType.ActivityCalendar)
                        {
                            continue;
                        }
                    }

                    converter.UpdateOBISCodeInformation(obj);
                    string obis = obj.LogicalName;
                    string name = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obis}" : obj.Description;
                    string val = "";
                    
                    if (obj.ObjectType == ObjectType.Register || 
                        obj.ObjectType == ObjectType.Data || 
                        obj.ObjectType == ObjectType.Clock || 
                        obj.ObjectType == ObjectType.ExtendedRegister || 
                        obj.ObjectType == ObjectType.DemandRegister ||
                        obj.ObjectType == ObjectType.IecHdlcSetup ||
                        obj.ObjectType == ObjectType.TcpUdpSetup ||
                        obj.ObjectType == ObjectType.Ip4Setup ||
                        obj.ObjectType == ObjectType.MacAddressSetup ||
                        obj.ObjectType == ObjectType.AssociationLogicalName ||
                        obj.ObjectType == ObjectType.ScriptTable ||
                        obj.ObjectType == ObjectType.ProfileGeneric ||
                        obj.ObjectType == ObjectType.ActionSchedule ||
                        obj.ObjectType == ObjectType.ActivityCalendar)
                    {
                        try
                        {
                            val = ReadObjectValue(obj);
                        }
                        catch (Exception ex)
                        {
                            val = $"Error: {ex.Message}";
                        }
                    }

                    list.Add(new DiscoveredParameter
                    {
                        Name = name,
                        ObisCode = obis,
                        ObjectType = obj.ObjectType.ToString(),
                        Value = val
                    });
                }
            }
            return list;
        }

        public bool ReadDataBlock(byte[][] data, GXReplyData reply)
        {
            if (data == null)
            {
                return true;
            }
            foreach (byte[] it in data)
            {
                reply.Clear();
                ReadDataBlock(it, reply);
            }
            return reply.Error == 0;
        }

        public void ReadDataBlock(byte[]? data, GXReplyData reply)
        {
            ReadDLMSPacket(data, reply);
            lock (_media.Synchronous)
            {
                while (reply.IsMoreData &&
                    (_client.ConnectionState != ConnectionState.None ||
                    _client.PreEstablishedConnection))
                {
                    if (reply.IsStreaming())
                    {
                        data = null;
                    }
                    else
                    {
                        data = _client.ReceiverReady(reply);
                    }
                    ReadDLMSPacket(data, reply);
                }
            }
        }

        public void ReadDLMSPacket(byte[]? data, GXReplyData reply)
        {
            if (data == null && !reply.IsStreaming())
            {
                return;
            }
            GXReplyData notify = new GXReplyData();
            reply.Error = 0;
            object? eop = (byte)0x7E;
            
            if (_client.InterfaceType != InterfaceType.HDLC &&
                _client.InterfaceType != InterfaceType.HdlcWithModeE)
            {
                eop = null;
            }
            int pos = 0;
            bool succeeded = false;
            var rd = new GXByteBuffer();
            var p = new ReceiveParameters<byte[]>()
            {
                Eop = eop,
                Count = _client.GetFrameSize(rd),
                AllData = true,
                WaitTime = WaitTime,
            };
            lock (_media.Synchronous)
            {
                while (!succeeded && pos < RetryCount)
                {
                    if (!reply.IsStreaming())
                    {
                        p.Reply = null!;
                        if (data != null)
                        {
                            _media.Send(data, null);
                        }
                    }
                    succeeded = _media.Receive(p);
                    if (!succeeded)
                    {
                        if (++pos >= RetryCount)
                        {
                            throw new Exception("Failed to receive reply from the device in given time.");
                        }
                        if (p.Eop == null)
                        {
                            p.Count = 1;
                        }
                    }
                }
                rd = new GXByteBuffer(p.Reply);
                pos = 0;
                while (!_client.GetData(rd, reply, notify))
                {
                    p.Reply = null!;
                    if (notify.IsComplete && notify.Data.Data != null)
                    {
                        if (!notify.IsMoreData)
                        {
                            notify.Clear();
                            continue;
                        }
                    }
                    if (p.Eop == null)
                    {
                        p.Count = _client.GetFrameSize(rd);
                    }
                    while (!_media.Receive(p))
                    {
                        if (++pos >= RetryCount)
                        {
                            throw new Exception("Failed to receive reply from the device in given time.");
                        }
                        p.Reply = null!;
                        if (data != null)
                        {
                            _media.Send(data, null);
                        }
                    }
                    rd.Set(p.Reply);
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_isConnected && _client != null && _client.ConnectionState != ConnectionState.None)
                {
                    var reply = new GXReplyData();
                    byte[][] closeCmd = null;

                    if (_client.InterfaceType == InterfaceType.WRAPPER)
                    {
                        closeCmd = _client.ReleaseRequest();
                    }
                    else
                    {
                        var disconnectFrame = _client.DisconnectRequest();
                        if (disconnectFrame != null)
                        {
                            closeCmd = new[] { disconnectFrame };
                        }
                    }

                    if (closeCmd != null)
                    {
                        ReadDataBlock(closeCmd, reply);
                    }
                }
            }
            catch { }
            finally
            {
                _isConnected = false;
                try { _media.Close(); } catch { }
            }
        }

        public void Reconnect()
        {
            // Fully close existing TCP session before reconnecting
            _isConnected = false;
            try { _media.Close(); } catch { }
            Thread.Sleep(2000); // Give meter time to release the session
            Connect();
        }

        public void Dispose()
        {
            Disconnect();
            _media?.Dispose();
        }
    }
}
