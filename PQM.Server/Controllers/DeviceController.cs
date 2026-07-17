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
using System;
using System.Collections.Generic;
using System.Linq;

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

        public DeviceController(
            ILogger<DeviceController> logger,
            IDeviceService deviceService,
            IConfiguration configuration,
            DLMSSessionManager sessionManager)
        {
            _logger = logger;
            _deviceService = deviceService;
            _configuration = configuration;
            _sessionManager = sessionManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get()
        {
            var data = _deviceService.GetDevices().ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpGet("{id}")]
        public ActionResult Get(int id)
        {
            try
            {
                var data = _deviceService.GetDevices().FirstOrDefault(x => x.Id == id);
                if (data == null)
                {
                    return NotFound(new { error = "Device not found." });
                }
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] Device device)
        {
            try
            {
                if (device == null)
                {
                    return BadRequest(new { error = "Invalid device payload." });
                }

                device.CreatedDate = DateTime.UtcNow;
                var id = _deviceService.AddDevice(device);
                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = id;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpPut]
        public ActionResult Put([FromBody] Device device)
        {
            try
            {
                if (device == null)
                {
                    return BadRequest(new { error = "Invalid device payload." });
                }

                var success = _deviceService.UpdateDevice(device);
                if (!success)
                {
                    return NotFound(new { error = "Device not found." });
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = success;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                var success = _deviceService.DeleteDevice(id);
                if (!success)
                {
                    return NotFound(new { error = "Device not found." });
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = success;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Data = null;
                _apiResponse.Errors = new List<string> { ex.Message };
                return Ok(_apiResponse);
            }
        }

        [HttpPost("{id}/discover-parameters")]
        public IActionResult DiscoverParameters(int id, [FromQuery] string? objectType)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 16);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;

            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                var parameters = reader.GetAssociationViewWithValues(objectType);

                if (parameters.Any(p => p.Value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(p.Value)))
                {
                    throw new System.IO.IOException("Connection lost during parameter discovery");
                }

                using var db = new DataContext(_connectionString);

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

            using var db = new DataContext(_connectionString);
            var param = db.Parameter.FirstOrDefault(p => p.Id == parameterId);
            if (param == null || string.IsNullOrEmpty(param.ObisCode))
                return Error("Parameter not found", System.Net.HttpStatusCode.NotFound);

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                string value = reader.ReadRegister(param.ObisCode, param.Name ?? "");

                if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                {
                    throw new System.IO.IOException(value);
                }

                if (param.ObisCode.StartsWith("0.0.96.11."))
                {
                    value = DecodeEventStatusBitmask(param.ObisCode, value);
                }

                var pv = new ParameterValue
                {
                    DeviceId = id,
                    ParameterId = parameterId,
                    Value = value,
                    Timestamp = DateTime.UtcNow
                };

                db.ParameterValue.Add(pv);
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

            using var db = new DataContext(_connectionString);
            var param = db.Parameter.FirstOrDefault(p => p.Id == objectId);
            if (param == null || string.IsNullOrEmpty(param.ObisCode))
                return Error("Parameter/Object not found", System.Net.HttpStatusCode.NotFound);

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);

                if (!Enum.TryParse<ObjectType>(param.ObjectType ?? "Register", out var objectType))
                {
                    objectType = ObjectType.Register;
                }

                var obj = GXDLMSClient.CreateObject(objectType);
                obj.LogicalName = param.ObisCode;

                string value = reader.ReadObjectAttribute(obj, 2);
                if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                {
                    throw new System.IO.IOException(value);
                }

                if (param.ObisCode.StartsWith("0.0.96.11."))
                {
                    value = DecodeEventStatusBitmask(param.ObisCode, value);
                }

                var pv = new ParameterValue
                {
                    DeviceId = id,
                    ParameterId = objectId,
                    Value = value ?? "",
                    Timestamp = DateTime.UtcNow
                };
                db.ParameterValue.Add(pv);
                db.SaveChanges();

                var results = new List<object>
                {
                    new
                    {
                        pv.Id,
                        ObjectId = objectId,
                        pv.ParameterId,
                        AttributeId = 2,
                        param.Name,
                        DataType = param.ObjectType,
                        AccessType = "Read",
                        pv.Value,
                        pv.Timestamp
                    }
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = results;
                return Ok(_apiResponse);
            });
        }

        [HttpPost("{id}/read-objects")]
        public IActionResult ReadObjects(int id, [FromBody] List<int> objectIds)
        {
            _apiResponse.Errors.Clear();

            if (objectIds == null || !objectIds.Any())
            {
                return Error("No object IDs provided", System.Net.HttpStatusCode.BadRequest);
            }

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            using var db = new DataContext(_connectionString);
            var paramsToRead = db.Parameter.Where(p => objectIds.Contains(p.Id) && !string.IsNullOrEmpty(p.ObisCode)).ToList();

            return ExecuteWithAutoReconnect(id, device, () =>
            {
                var reader = GetOrCreateSession(id, device);
                var results = new List<object>();

                foreach (var param in paramsToRead)
                {
                    if (!Enum.TryParse<ObjectType>(param.ObjectType ?? "Register", out var objectType))
                    {
                        objectType = ObjectType.Register;
                    }

                    var obj = GXDLMSClient.CreateObject(objectType);
                    obj.LogicalName = param.ObisCode;

                    string value = reader.ReadObjectAttribute(obj, 2);
                    if (value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && IsConnectionLostError(value))
                    {
                        throw new System.IO.IOException(value);
                    }

                    if (param.ObisCode.StartsWith("0.0.96.11."))
                    {
                        value = DecodeEventStatusBitmask(param.ObisCode, value);
                    }

                    var pv = new ParameterValue
                    {
                        DeviceId = id,
                        ParameterId = param.Id,
                        Value = value ?? "",
                        Timestamp = DateTime.UtcNow
                    };
                    db.ParameterValue.Add(pv);

                    results.Add(new
                    {
                        pv.Id,
                        ObjectId = param.Id,
                        pv.ParameterId,
                        AttributeId = 2,
                        param.Name,
                        DataType = param.ObjectType,
                        AccessType = "Read",
                        pv.Value,
                        pv.Timestamp
                    });
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

                var obj = GXDLMSClient.CreateObject(ObjectType.Register);
                obj.LogicalName = request.ObisCode;

                reader.WriteRegister(request.ObisCode, request.Value, request.AttributeId);

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = "Write successful";
                return Ok(_apiResponse);
            });
        }

        [HttpGet("{id}/configuration")]
        public IActionResult GetConfiguration(int id)
        {
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = new
            {
                hdlc = new List<object>(),
                tcp = new List<object>(),
                ip4 = new List<object>(),
                mac = new List<object>(),
                association = new List<object>()
            };
            return Ok(_apiResponse);
        }

        private DLMSReader GetOrCreateSession(int id, Device device)
        {
            var session = _sessionManager.GetSession(id);
            if (session != null)
                return session;

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 16);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            if (!Enum.TryParse(authStr, true, out Authentication authentication))
                authentication = Authentication.None;

            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            return _sessionManager.Connect(
                id,
                device.IP,
                device.PORT,
                clientAddress,
                serverAddress,
                authentication,
                password,
                useLogicalNameReferencing,
                standard
            );
        }

        private IActionResult ExecuteWithAutoReconnect(int id, Device device, Func<IActionResult> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Connection lost for device {DeviceId}. Attempting auto-reconnect...", id);
                try
                {
                    _sessionManager.Disconnect(id);
                    var freshReader = GetOrCreateSession(id, device);
                    freshReader.Connect();
                    return action();
                }
                catch (Exception reconnectEx)
                {
                    _logger.LogError(reconnectEx, "Failed to auto-reconnect and retry for device {DeviceId}.", id);
                    return Error($"Device connection lost and reconnection failed: {reconnectEx.Message}", System.Net.HttpStatusCode.GatewayTimeout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing meter request for device {DeviceId}.", id);
                return Error(ex.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        private bool IsConnectionLostError(string value)
        {
            return value.Contains("Connection reset", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("not connected", StringComparison.OrdinalIgnoreCase);
        }

        private string DecodeEventStatusBitmask(string obisCode, string bitmaskHex)
        {
            if (string.IsNullOrWhiteSpace(bitmaskHex) || bitmaskHex.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                return bitmaskHex;

            try
            {
                if (bitmaskHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    bitmaskHex = bitmaskHex.Substring(2);

                long val = Convert.ToInt64(bitmaskHex, 16);
                var activeEvents = new List<string>();

                var labelMap = obisCode.EndsWith(".1.255") ? CurrentEventLabels :
                               obisCode.EndsWith(".2.255") ? PowerEventLabels :
                               obisCode.EndsWith(".3.255") ? TransactionEventLabels :
                               VoltageEventLabels;

                foreach (var pair in labelMap)
                {
                    if ((val & (1L << pair.Key)) != 0)
                    {
                        activeEvents.Add(pair.Value);
                    }
                }

                return activeEvents.Any() ? string.Join(", ", activeEvents) : "No Events Active";
            }
            catch
            {
                return bitmaskHex;
            }
        }

        private IActionResult Error(string message, System.Net.HttpStatusCode statusCode)
        {
            _apiResponse.Status = false;
            _apiResponse.StatusCode = statusCode;
            _apiResponse.Errors.Add(message);
            return Ok(_apiResponse);
        }

        private static readonly Dictionary<int, string> VoltageEventLabels = new()
        {
            { 0, "R-Phase - Voltage Missing - Occurrence" },
            { 1, "R-Phase - Voltage Missing - Restoration" },
            { 2, "Y-Phase - Voltage Missing - Occurrence" },
            { 3, "Y-Phase - Voltage Missing - Restoration" },
            { 4, "B-Phase - Voltage Missing - Occurrence" },
            { 5, "B-Phase - Voltage Missing - Restoration" },
            { 6, "Over Voltage in any Phase - Occurrence" },
            { 7, "Over Voltage in any Phase - Restoration" },
            { 8, "Low Voltage in any Phase - Occurrence" },
            { 9, "Low Voltage in any Phase - Restoration" },
            { 10, "Voltage Unbalance - Occurrence" },
            { 11, "Voltage Unbalance - Restoration" }
        };

        private static readonly Dictionary<int, string> CurrentEventLabels = new()
        {
            { 4, "R Phase - Current reverse - Occurrence" },
            { 5, "R Phase - Current reverse - Restoration" },
            { 8, "Y Phase - Current reverse - Occurrence" },
            { 9, "Y Phase - Current reverse - Restoration" },
            { 10, "B Phase - Current reverse - Occurrence" },
            { 11, "B Phase - Current reverse - Restoration" },
            { 7, "Current Unbalance - Occurrence" },
            { 6, "Current Unbalance - Restoration" },
            { 0, "Current bypass - Occurrence" },
            { 1, "Current bypass - Restoration" },
            { 2, "Over current in any phase - Occurrence" },
            { 3, "Over current in any phase - Restoration" }
        };

        private static readonly Dictionary<int, string> PowerEventLabels = new()
        {
            { 0, "Power failure - Occurrence" },
            { 1, "Power failure - Restoration" }
        };

        private static readonly Dictionary<int, string> TransactionEventLabels = new()
        {
            { 0, "Real Time Clock - Date and Time" },
            { 1, "Demand Integration Period" },
            { 2, "Profile Capture Period" },
            { 3, "Single-action Schedule for Billing Dates" },
            { 4, "Activity Calendar Time Zones" },
            { 5, "New Firmware Activated" },
            { 6, "Load limit (kW) set" },
            { 7, "Enabled - load limit function" },
            { 8, "Disabled - load limit function" },
            { 9, "LLS secret (MR) change" },
            { 10, "HLS key (US) change" },
            { 11, "HLS key (FW) change" },
            { 12, "Global key change(encryption and authentication)" },
            { 13, "ESWF change" },
            { 14, "MD reset" },
            { 15, "Single Action Schedule for Image Activation" },
            { 16, "Passive Relay time." }
        };
    }
}
