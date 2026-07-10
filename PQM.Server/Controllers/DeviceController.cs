using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;
using PQM.Server.Models;
using Microsoft.AspNetCore.SignalR;
using PQM.Server.Hubs;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly DLMSSessionManager _sessionManager;
        private readonly IHubContext<MeterHub> _hubContext;

        public DeviceController(
            ILogger<DeviceController> logger,
            IDeviceService deviceService,
            IConfiguration configuration,
            DLMSSessionManager sessionManager,
            IHubContext<MeterHub> hubContext)
        {
            _logger = logger;
            _deviceService = deviceService;
            _configuration = configuration;
            _sessionManager = sessionManager;
            _hubContext = hubContext;

            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        // -------------------- NOTIFY DEVICE UPDATE (SignalR Broadcast) --------------------
        [HttpPost("{deviceId}/notify-update")]
        public async Task<IActionResult> NotifyUpdate(int deviceId)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("MeterUpdated", deviceId);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = $"Real-time notification broadcasted successfully for device {deviceId}.";
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        // -------------------- GET DEVICES --------------------
        [HttpGet]
        public IActionResult Get()
        {
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = _deviceService.GetDevices().ToList();
            return Ok(_apiResponse);
        }

        // -------------------- GET DEVICE CONFIGURATION --------------------
        [HttpGet("{id}/configuration")]
        public IActionResult GetConfiguration(int id)
        {
            try
            {
                using var dbContext = new DataContext(_connectionString);

                var hdlc = dbContext.IecHdlcSetup.Where(x => x.DeviceId == id).ToList();
                var tcp = dbContext.TcpUdpSetup.Where(x => x.DeviceId == id).ToList();
                var ip4 = dbContext.Ip4Setup.Where(x => x.DeviceId == id).ToList();
                var mac = dbContext.MacAddressSetup.Where(x => x.DeviceId == id).ToList();
                var scripts = dbContext.ScriptTable.Where(x => x.DeviceId == id).ToList();
                var schedules = dbContext.ActionSchedule.Where(x => x.DeviceId == id).ToList();

                var configData = new
                {
                    Hdlc = hdlc,
                    Tcp = tcp,
                    Ip4 = ip4,
                    Mac = mac,
                    Scripts = scripts,
                    Schedules = schedules
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = configData;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        // -------------------- ADD DEVICE --------------------
        [HttpPost]
        public IActionResult Post([FromBody] Device device)
        {
            _apiResponse.Errors.Clear();

            if (!RequiredFieldValidation(device) || !IsDeviceAlreadyExist(device))
            {
                _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                return Ok(_apiResponse);
            }

            device.CreatedDate = DateTime.UtcNow;
            device.Id = _deviceService.AddDevice(device);

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = device;
            return Ok(_apiResponse);
        }

        // -------------------- CONNECT DEVICE --------------------
        [HttpPost("{id}/connect")]
        public IActionResult Connect(int id)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 1);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;

            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            try
            {
                _sessionManager.Connect(id, device.IP, device.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard);
                
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = $"Successfully connected to device {device.Name} at {device.IP}:{device.PORT}.";
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                return Error($"Failed to connect to device: {ex.Message}", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        // -------------------- DISCONNECT DEVICE --------------------
        [HttpPost("{id}/disconnect")]
        public IActionResult Disconnect(int id)
        {
            _apiResponse.Errors.Clear();
            _sessionManager.Disconnect(id);

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = "Disconnected successfully.";
            return Ok(_apiResponse);
        }

        // -------------------- DISCOVER PARAMETERS --------------------
        [HttpPost("{id}/discover-parameters")]
        public IActionResult DiscoverParameters(int id, [FromQuery] string? objectType = null)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 1);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;

            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            // Run the discovery — if TCP was dropped silently by meter, auto-reconnect and retry once
            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                var parameters = reader.GetAssociationViewWithValues(objectType);

                // If any parameters returned had connection errors, trigger auto-reconnect
                if (parameters.Any(p => p.Value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(p.Value)))
                {
                    throw new System.IO.IOException("Connection lost during parameter discovery");
                }

                var allDlmsObjects = new List<Gurux.DLMS.Objects.GXDLMSObject>();
                if (reader.Objects != null)
                {
                    foreach (var obj in reader.Objects)
                    {
                        allDlmsObjects.Add(obj);
                    }
                }

                using var db = new DataContext(_connectionString);

                // Backward compatibility: Populate the flat Parameter table
                var existingParams = db.Parameter
                    .Where(p => !string.IsNullOrEmpty(p.ObisCode))
                    .ToList();

                var obisMap = existingParams
                    .ToDictionary(p => p.ObisCode!, p => p);

                foreach (var p in parameters)
                {
                    if (!obisMap.ContainsKey(p.ObisCode))
                    {
                        var param = new Parameter
                        {
                            Name = p.Name,
                            ObisCode = p.ObisCode,
                            ObjectType = p.ObjectType,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        db.Parameter.Add(param);
                        obisMap[p.ObisCode] = param;
                    }
                }
                db.SaveChanges();

                // DYNAMIC CLONE OF GURUX DIRECTOR: Seed ConnectedHeader, DLMSObject, ObjectParameter
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Get existing headers for this device
                        var existingHeaders = db.ConnectedHeader.Where(h => h.DeviceId == id).ToList();
                        var headerIds = existingHeaders.Select(h => h.Id).ToList();

                        // 2. Get existing objects for these headers
                        var existingObjects = db.DLMSObject.Where(o => headerIds.Contains(o.HeaderId)).ToList();
                        var objectIds = existingObjects.Select(o => o.Id).ToList();

                        // 3. Delete existing ParameterValues, ObjectParameters, DLMSObjects, ConnectedHeaders
                        var parameterIds = db.ObjectParameter.Where(p => objectIds.Contains(p.ObjectId)).Select(p => p.Id).ToList();
                        var valuesToDelete = db.ParameterValue.Where(v => parameterIds.Contains(v.ParameterId));
                        db.ParameterValue.RemoveRange(valuesToDelete);

                        var paramsToDelete = db.ObjectParameter.Where(p => objectIds.Contains(p.ObjectId));
                        db.ObjectParameter.RemoveRange(paramsToDelete);

                        db.DLMSObject.RemoveRange(existingObjects);
                        db.ConnectedHeader.RemoveRange(existingHeaders);
                        db.SaveChanges();

                        // 4. Group discovered objects by ObjectType and seed new records
                        var groupedObjects = allDlmsObjects.GroupBy(o => o.ObjectType);
                        foreach (var group in groupedObjects)
                        {
                            var friendlyName = DLMSReader.GetFriendlyClassName(group.Key);
                            var header = new ConnectedHeader
                            {
                                DeviceId = id,
                                Name = friendlyName
                            };
                            db.ConnectedHeader.Add(header);
                            db.SaveChanges(); // Populate header.Id

                            foreach (var obj in group)
                            {
                                var dlmsObj = new DLMSObject
                                {
                                    HeaderId = header.Id,
                                    Name = string.IsNullOrEmpty(obj.Description) ? $"{obj.ObjectType} - {obj.LogicalName}" : obj.Description,
                                    ObisCode = obj.LogicalName,
                                    ObjectType = obj.ObjectType.ToString()
                                };
                                db.DLMSObject.Add(dlmsObj);
                                db.SaveChanges(); // Populate dlmsObj.Id

                                foreach (var attr in obj.Attributes)
                                {
                                    var objParam = new ObjectParameter
                                    {
                                        ObjectId = dlmsObj.Id,
                                        AttributeId = attr.Index,
                                        Name = attr.Name ?? $"Attribute {attr.Index}",
                                        DataType = attr.Type.ToString(),
                                        AccessType = attr.Access.ToString()
                                    };
                                    db.ObjectParameter.Add(objParam);
                                }
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Failed to dynamically save discovered objects for device {DeviceId}", id);
                        throw;
                    }
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = parameters;
                return Ok(_apiResponse);
            });
        }

        [HttpPost("{id}/read-parameter/{parameterId}")]
        public IActionResult ReadParameter(int id, int parameterId)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                using var db = new DataContext(_connectionString);
                var param = db.Parameter.FirstOrDefault(p => p.Id == parameterId);

                if (param == null || string.IsNullOrEmpty(param.ObisCode))
                    return Error("Invalid parameter", System.Net.HttpStatusCode.NotFound);

                var reader = GetOrCreateSession(id, device);
                string value = reader.ReadRegister(param.ObisCode, param.Name ?? "");

                if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                {
                    throw new System.IO.IOException(value);
                }

                if (param.ObisCode.StartsWith("0.0.96.11."))
                {
                    value = DecodeEventStatusBitmask(db, param.ObisCode, value);
                }

                db.DeviceLog.Add(new DeviceLog
                {
                    DeviceId = id,
                    ParameterId = parameterId,
                    Value = value,
                    DateStamp = DateTime.UtcNow
                });

                SaveTypedReading(db, id, param.Name ?? "", param.ObjectType ?? "", value, 2);
                db.SaveChanges();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = value;
                return Ok(_apiResponse);
            });
        }

        [HttpPost("{id}/read-object/{objectId}")]
        public IActionResult ReadObject(int id, int objectId)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                using var db = new DataContext(_connectionString);
                var dlmsObject = db.DLMSObject.FirstOrDefault(o => o.Id == objectId);
                if (dlmsObject == null)
                    return Error("DLMS Object not found", System.Net.HttpStatusCode.NotFound);

                var parameters = db.ObjectParameter.Where(p => p.ObjectId == objectId).ToList();
                if (!parameters.Any())
                    return Error("No parameters found for the object", System.Net.HttpStatusCode.NotFound);

                var reader = GetOrCreateSession(id, device);

                Gurux.DLMS.Objects.GXDLMSObject? obj = null;
                if (reader.Objects != null)
                {
                    obj = reader.Objects.FirstOrDefault(o => o.LogicalName == dlmsObject.ObisCode);
                }

                if (obj == null)
                {
                    if (!Enum.TryParse<ObjectType>(dlmsObject.ObjectType, out var objectType))
                    {
                        objectType = ObjectType.Register;
                    }
                    obj = GXDLMSClient.CreateObject(objectType);
                    obj.LogicalName = dlmsObject.ObisCode;
                }

                var results = new List<object>();
                foreach (var param in parameters)
                {
                    string value = reader.ReadObjectAttribute(obj, param.AttributeId);
                    if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                    {
                        throw new System.IO.IOException(value);
                    }

                    if (dlmsObject.ObisCode.StartsWith("0.0.96.11.") && param.AttributeId == 2)
                    {
                        value = DecodeEventStatusBitmask(db, dlmsObject.ObisCode, value);
                    }

                    var pv = new ParameterValue
                    {
                        ParameterId = param.Id,
                        Value = value ?? "",
                        Timestamp = DateTime.UtcNow
                    };
                    db.ParameterValue.Add(pv);
                    results.Add(new
                    {
                        pv.Id,
                        param.ObjectId,
                        pv.ParameterId,
                        AttributeId = param.AttributeId,
                        param.Name,
                        param.DataType,
                        param.AccessType,
                        pv.Value,
                        pv.Timestamp
                    });

                    SaveTypedReading(db, id, dlmsObject.Name ?? "", dlmsObject.ObjectType, value ?? "", param.AttributeId);
                }

                db.SaveChanges();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = results;
                return Ok(_apiResponse);
            });
        }

        public class WriteObjectRequest
        {
            public required string ObisCode { get; set; }
            public required string Value { get; set; }
            public int AttributeId { get; set; } = 2;
        }

        [HttpPost("{id}/write-object")]
        public IActionResult WriteObject(int id, [FromBody] WriteObjectRequest request)
        {
            _apiResponse.Errors.Clear();

            if (request == null || string.IsNullOrEmpty(request.ObisCode))
            {
                return Error("Invalid write request payload.", System.Net.HttpStatusCode.BadRequest);
            }

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                reader.WriteRegister(request.ObisCode, request.Value, request.AttributeId);

                // Broadcast change through SignalR
                _hubContext.Clients.All.SendAsync("MeterUpdated", id);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = $"Successfully wrote '{request.Value}' to parameter {request.ObisCode} (Attribute {request.AttributeId}).";
                return Ok(_apiResponse);
            });
        }

        [HttpPost("{id}/read-objects")]
        public IActionResult ReadObjects(int id, [FromBody] List<int> objectIds)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            if (objectIds == null || !objectIds.Any())
                return Error("No object IDs provided", System.Net.HttpStatusCode.BadRequest);

            using var db = new DataContext(_connectionString);
            
            // Get all requested DLMS objects
            var dlmsObjects = db.DLMSObject.Where(o => objectIds.Contains(o.Id)).ToList();
            if (!dlmsObjects.Any())
                return Error("No valid DLMS Objects found", System.Net.HttpStatusCode.NotFound);

            // Get parameters for these objects
            var dbObjectIds = dlmsObjects.Select(o => o.Id).ToList();
            var parameters = db.ObjectParameter.Where(p => dbObjectIds.Contains(p.ObjectId)).ToList();

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 1);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;

            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            // Dictionary to store results grouped by objectId
            // Run the scan — if TCP was dropped silently by meter, auto-reconnect and retry once
            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                var batchResults = new Dictionary<int, List<object>>();

                foreach (var dlmsObject in dlmsObjects)
                {
                    var objectParameters = parameters.Where(p => p.ObjectId == dlmsObject.Id).ToList();
                    if (!objectParameters.Any()) continue;

                    Gurux.DLMS.Objects.GXDLMSObject? obj = null;
                    if (reader.Objects != null)
                        obj = reader.Objects.FirstOrDefault(o => o.LogicalName == dlmsObject.ObisCode);

                    if (obj == null)
                    {
                        if (!Enum.TryParse<ObjectType>(dlmsObject.ObjectType, out var ot))
                            ot = ObjectType.Register;
                        obj = GXDLMSClient.CreateObject(ot);
                        obj.LogicalName = dlmsObject.ObisCode;
                    }

                    var objResults = new List<object>();
                    foreach (var param in objectParameters)
                    {
                        // Let connection errors bubble up so ExecuteWithAutoReconnect can retry
                        string value = reader.ReadObjectAttribute(obj, param.AttributeId);
                        if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                        {
                            throw new System.IO.IOException(value);
                        }

                        if (dlmsObject.ObisCode.StartsWith("0.0.96.11.") && param.AttributeId == 2)
                        {
                            value = DecodeEventStatusBitmask(db, dlmsObject.ObisCode, value);
                        }

                        var pv = new ParameterValue
                        {
                            ParameterId = param.Id,
                            Value = value ?? "",
                            Timestamp = DateTime.UtcNow
                        };
                        db.ParameterValue.Add(pv);
                        objResults.Add(new
                        {
                            pv.Id, param.ObjectId, pv.ParameterId,
                            AttributeId = param.AttributeId,
                            param.Name, param.DataType, param.AccessType,
                            pv.Value, pv.Timestamp
                        });
                        SaveTypedReading(db, id, dlmsObject.Name ?? "", dlmsObject.ObjectType, value ?? "", param.AttributeId);
                    }
                    batchResults[dlmsObject.Id] = objResults;
                }

                db.SaveChanges();
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = batchResults;
                return Ok(_apiResponse);
            });
        }

        // -------------------- HELPERS --------------------

        /// <summary>
        /// Runs 'action'. If the connection dropped (meter closed idle TCP), it reconnects once and retries.
        /// This way the user never sees a "Failed to receive reply" error after waiting.
        /// </summary>
        private IActionResult ExecuteWithAutoReconnect(int deviceId, Device device, Func<IActionResult> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (IsConnectionLostError(ex.Message))
            {
                _logger.LogWarning("[Session] Connection lost for device {DeviceId}: {Msg}. Reconnecting...", deviceId, ex.Message);
                _sessionManager.Disconnect(deviceId);

                try
                {
                    // Force a fresh connection and retry once
                    return action();
                }
                catch (Exception retryEx)
                {
                    _sessionManager.Disconnect(deviceId);
                    return Error($"DLMS Error after reconnect: {retryEx.Message}", System.Net.HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _sessionManager.Disconnect(deviceId);
                return Error($"DLMS Error: {ex.Message}", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        private IActionResult Error(string message, System.Net.HttpStatusCode code)
        {
            _apiResponse.Status = false;
            _apiResponse.StatusCode = code;
            _apiResponse.Errors.Add(message);
            return Ok(_apiResponse);
        }

        private bool RequiredFieldValidation(Device device)
        {
            if (string.IsNullOrWhiteSpace(device.Name)) _apiResponse.Errors.Add("Name required");
            if (string.IsNullOrWhiteSpace(device.IP)) _apiResponse.Errors.Add("IP required");
            if (device.PORT <= 0) _apiResponse.Errors.Add("Port required");
            return !_apiResponse.Errors.Any();
        }

        private DLMSReader GetOrCreateSession(int deviceId, Device device)
        {
            return GetOrReconnectSession(deviceId, device);
        }

        /// <summary>
        /// Returns a live DLMS session. If the cached session is stale/dead (TCP dropped by meter
        /// after inactivity), it silently reconnects and returns a fresh session.
        /// The user never sees a "Failed to receive reply" error caused by an idle connection.
        /// </summary>
        private DLMSReader GetOrReconnectSession(int deviceId, Device device)
        {
            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 1);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;
            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            // Try existing cached session first
            var reader = _sessionManager.GetSession(deviceId);
            if (reader != null)
                return reader;

            // No session — create a fresh one
            _logger.LogInformation("[Session] Connecting to device {DeviceId} at {IP}:{Port}", deviceId, device.IP, device.PORT);
            return _sessionManager.Connect(deviceId, device.IP, device.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard);
        }

        private string DecodeEventStatusBitmask(DataContext db, string obisCode, string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue) || !int.TryParse(rawValue.Trim(), out int numericValue))
            {
                return rawValue;
            }

            var mappings = db.EventStatusMapping
                .Where(m => m.ObisCode == obisCode)
                .ToList();

            if (!mappings.Any())
            {
                return rawValue;
            }

            var activeEvents = new List<string>();
            foreach (var mapping in mappings)
            {
                if ((numericValue & (1 << mapping.BitIndex)) != 0)
                {
                    activeEvents.Add(mapping.EventCode.ToString());
                }
            }

            if (activeEvents.Any())
            {
                return string.Join(",", activeEvents);
            }

            return "0";
        }

        /// <summary>
        /// Checks if an exception means the TCP connection died (meter closed idle socket).
        /// </summary>
        private static bool IsConnectionLostError(string message)
        {
            return message.Contains("Failed to receive reply", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || message.Contains("socket", StringComparison.OrdinalIgnoreCase)
                || message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || message.Contains("disconnected", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDeviceAlreadyExist(Device device)
        {
            var devices = _deviceService.GetDevices();
            if (devices.Any(d => d.Id != device.Id && d.IP == device.IP && d.PORT == device.PORT))
            {
                _apiResponse.Errors.Add("Device already exists");
                return false;
            }
            return true;
        }

        private void SaveTypedReading(DataContext db, int deviceId, string name, string objectType, string value, int attributeId)
        {
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!IsPrimaryAttributeRead(objectType, attributeId))
            {
                return;
            }

            try
            {
                switch (NormalizeObjectType(objectType))
                {
                    case "Register":
                    case "ExtendedRegister":
                    case "DemandRegister":
                        db.Register.Add(new Register
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "Data":
                        db.Data.Add(new PQM.Core.Entities.Data
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "IecHdlcSetup":
                        db.IecHdlcSetup.Add(new IecHdlcSetup
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "TcpUdpSetup":
                        db.TcpUdpSetup.Add(new TcpUdpSetup
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "Ip4Setup":
                        db.Ip4Setup.Add(new Ip4Setup
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "MacAddressSetup":
                        db.MacAddressSetup.Add(new MacAddressSetup
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "AssociationLogicalName":
                        db.AssociationLogicalName.Add(new AssociationLogicalName
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "Clock":
                        db.Clock.Add(new Clock
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "ScriptTable":
                        db.ScriptTable.Add(new ScriptTable
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "ProfileGeneric":
                        db.ProfileGeneric.Add(new ProfileGeneric
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "ActionSchedule":
                        db.ActionSchedule.Add(new ActionSchedule
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                    case "ActivityCalendar":
                        db.ActivityCalendar.Add(new ActivityCalendar
                        {
                            DeviceId = deviceId,
                            Name = name,
                            ObjectType = objectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save typed reading for {ObjectType}", objectType);
            }
        }

        private static bool IsPrimaryAttributeRead(string objectType, int attributeId)
        {
            var normalized = NormalizeObjectType(objectType);
            return normalized switch
            {
                "Register" => attributeId == 2,
                "ExtendedRegister" => attributeId == 2,
                "DemandRegister" => attributeId == 2,
                "Data" => attributeId == 2,
                "IecHdlcSetup" => attributeId == 2,
                "TcpUdpSetup" => attributeId == 2,
                "Ip4Setup" => attributeId == 2,
                "MacAddressSetup" => attributeId == 2,
                "AssociationLogicalName" => attributeId == 2,
                "Clock" => attributeId == 2,
                "ScriptTable" => attributeId == 2,
                "ProfileGeneric" => attributeId == 2,
                "ActionSchedule" => attributeId == 2,
                "ActivityCalendar" => attributeId == 2,
                _ => true
            };
        }

        private static string NormalizeObjectType(string objectType)
        {
            return objectType.Trim().Equals("lecHdlcSetup", StringComparison.OrdinalIgnoreCase)
                ? "IecHdlcSetup"
                : objectType.Trim();
        }
    }
}
