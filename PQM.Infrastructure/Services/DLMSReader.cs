using System;
using System.Collections.Generic;
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
        public string Name { get; set; }
        public string ObisCode { get; set; }
        public string ObjectType { get; set; }
        public string Value { get; set; }
    }

    public class DLMSReader : IDisposable
    {
        private readonly GXNet _media;
        private readonly GXDLMSClient _client;

        public int WaitTime { get; set; } = 5000; // ms
        public int RetryCount { get; set; } = 3;

        public GXDLMSObjectCollection Objects => _client.Objects;

        public DLMSReader(string ipAddress, int port, int clientAddress = 16, int serverAddress = 1, Authentication authentication = Authentication.None, string password = "", bool useLogicalNameReferencing = true, Standard standard = Standard.DLMS)
        {
            _media = new GXNet(NetworkType.Tcp, ipAddress, port);
            
            _client = new GXDLMSClient(useLogicalNameReferencing)
            {
                InterfaceType = InterfaceType.WRAPPER, // Standard wrapper for TCP
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
                GXDLMSObject obj = null;

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

        private string FormatValue(object? val)
        {
            if (val == null) return "";
            if (val is byte[] bytes)
            {
                return BitConverter.ToString(bytes).Replace("-", " ");
            }
            return val.ToString() ?? "";
        }

        public string ReadObjectValue(GXDLMSObject obj)
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
                    
                    if (obj is GXDLMSClock clock)
                    {
                        return clock.Time?.ToString() ?? "";
                    }
                    
                    if (obj is GXDLMSRegister reg)
                    {
                        return FormatValue(reg.Value);
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

        public string ReadObjectAttribute(GXDLMSObject obj, int attributeId)
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
                    
                    if (obj is GXDLMSClock clock && attributeId == 2)
                    {
                        return clock.Time?.ToString() ?? "";
                    }
                    
                    if (obj is GXDLMSRegister reg && attributeId == 2)
                    {
                        return FormatValue(reg.Value);
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

        public List<Parameter> GetAssociationView()
        {
            var list = new List<Parameter>();
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
                        obj.ObjectType != ObjectType.MacAddressSetup)
                    {
                        continue;
                    }

                    converter.UpdateOBISCodeInformation(obj);
                    string obis = obj.LogicalName;
                    string name = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obis}" : obj.Description;
                    
                    list.Add(new Parameter
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
                            obj.ObjectType != ObjectType.MacAddressSetup)
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
                        obj.ObjectType == ObjectType.MacAddressSetup)
                    {
                        val = ReadObjectValue(obj);
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

        public void ReadDataBlock(byte[] data, GXReplyData reply)
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

        public void ReadDLMSPacket(byte[] data, GXReplyData reply)
        {
            if (data == null && !reply.IsStreaming())
            {
                return;
            }
            GXReplyData notify = new GXReplyData();
            reply.Error = 0;
            object eop = (byte)0x7E;
            
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
                while (!succeeded && pos != 3)
                {
                    if (!reply.IsStreaming())
                    {
                        p.Reply = null;
                        _media.Send(data, null);
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
                try
                {
                    pos = 0;
                    while (!_client.GetData(rd, reply, notify))
                    {
                        p.Reply = null;
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
                            p.Reply = null;
                            _media.Send(data, null);
                        }
                        rd.Set(p.Reply);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                var reply = new GXReplyData();
                byte[] disconnect = _client.DisconnectRequest();
                if (disconnect != null)
                {
                    ReadDataBlock(disconnect, reply);
                }
            }
            catch { }
            finally
            {
                _media.Close();
            }
        }

        public void Dispose()
        {
            Disconnect();
            _media?.Dispose();
        }
    }
}
