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

        public DeviceController(
            ILogger<DeviceController> logger,
            IDeviceService deviceService,
            IConfiguration configuration)
        {
            _logger = logger;
            _deviceService = deviceService;
            _configuration = configuration;

            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
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

            List<DiscoveredParameter> parameters;
            using (var reader = new DLMSReader(device.IP, device.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard))
            {
                reader.Connect();
                parameters = reader.GetAssociationViewWithValues(objectType);
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
                return Error("Invalid parameter", System.Net.HttpStatusCode.NotFound);

            int clientAddress = _configuration.GetValue("DlmsSettings:ClientAddress", 1);
            int serverAddress = _configuration.GetValue("DlmsSettings:ServerAddress", 1);
            string authStr = _configuration.GetValue("DlmsSettings:Authentication", "None");
            string password = _configuration.GetValue("DlmsSettings:Password", "");
            bool useLogicalNameReferencing = _configuration.GetValue("DlmsSettings:UseLogicalNameReferencing", true);
            string standardStr = _configuration.GetValue("DlmsSettings:Standard", "DLMS");

            Enum.TryParse(authStr, true, out Authentication authentication);
            if (!Enum.TryParse(standardStr, true, out Standard standard))
                standard = Standard.DLMS;

            string value;
            using (var reader = new DLMSReader(device.IP, device.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard))
            {
                reader.Connect();
                value = reader.ReadRegister(param.ObisCode, param.Name ?? "");
            }

            db.DeviceLog.Add(new DeviceLog
            {
                DeviceId = id,
                ParameterId = parameterId,
                Value = value,
                DateStamp = DateTime.UtcNow
            });

            if (!string.IsNullOrEmpty(value) && !value.StartsWith("Error"))
            {
                if (param.ObjectType == "Register" || param.ObjectType == "ExtendedRegister" || param.ObjectType == "DemandRegister")
                {
                    try
                    {
                        var registerData = new Register
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.Register.Add(registerData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to Register table in ReadParameter");
                    }
                }
                else if (param.ObjectType == "Data")
                {
                    try
                    {
                        var dataVal = new PQM.Core.Entities.Data
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.Data.Add(dataVal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to Data table in ReadParameter");
                    }
                }
                else if (string.Equals(param.ObjectType, "IecHdlcSetup", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(param.ObjectType, "lecHdlcSetup", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var hdlcVal = new IecHdlcSetup
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.IecHdlcSetup.Add(hdlcVal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to IecHdlcSetup table in ReadParameter");
                    }
                }
                else if (string.Equals(param.ObjectType, "TcpUdpSetup", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var tcpVal = new TcpUdpSetup
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.TcpUdpSetup.Add(tcpVal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to TcpUdpSetup table in ReadParameter");
                    }
                }
                else if (string.Equals(param.ObjectType, "Ip4Setup", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var ipVal = new Ip4Setup
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.Ip4Setup.Add(ipVal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to Ip4Setup table in ReadParameter");
                    }
                }
                else if (string.Equals(param.ObjectType, "MacAddressSetup", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var macVal = new MacAddressSetup
                        {
                            DeviceId = id,
                            Name = param.Name ?? "",
                            ObjectType = param.ObjectType,
                            Value = value,
                            DateEntered = DateTime.UtcNow
                        };
                        db.MacAddressSetup.Add(macVal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save to MacAddressSetup table in ReadParameter");
                    }
                }
            }

            db.SaveChanges();

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = value;
            return Ok(_apiResponse);
        }

        // -------------------- READ OBJECT --------------------
        [HttpPost("{id}/read-object/{objectId}")]
        public IActionResult ReadObject(int id, int objectId)
        {
            _apiResponse.Errors.Clear();

            var device = _deviceService.GetDevices().FirstOrDefault(d => d.Id == id);
            if (device == null)
                return Error("Device not found", System.Net.HttpStatusCode.NotFound);

            using var db = new DataContext(_connectionString);
            var dlmsObject = db.DLMSObject.FirstOrDefault(o => o.Id == objectId);
            if (dlmsObject == null)
                return Error("DLMS Object not found", System.Net.HttpStatusCode.NotFound);

            var parameters = db.ObjectParameter.Where(p => p.ObjectId == objectId).ToList();
            if (!parameters.Any())
                return Error("No parameters found for the object", System.Net.HttpStatusCode.NotFound);

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

            var results = new List<object>();

            try
            {
                using (var reader = new DLMSReader(device.IP, device.PORT, clientAddress, serverAddress, authentication, password, useLogicalNameReferencing, standard))
                {
                    reader.Connect();

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

                    foreach (var param in parameters)
                    {
                        string value = reader.ReadObjectAttribute(obj, param.AttributeId);

                        if (!string.IsNullOrEmpty(value) && !value.StartsWith("Error"))
                        {
                            var pv = new ParameterValue
                            {
                                ParameterId = param.Id,
                                Value = value,
                                Timestamp = DateTime.UtcNow
                            };
                            db.ParameterValue.Add(pv);
                            results.Add(new
                            {
                                pv.Id,
                                pv.ParameterId,
                                AttributeId = param.AttributeId,
                                pv.Value,
                                pv.Timestamp
                            });

                            if (dlmsObject.ObjectType == "Register" || 
                                dlmsObject.ObjectType == "ExtendedRegister" || 
                                dlmsObject.ObjectType == "DemandRegister")
                            {
                                if (param.AttributeId == 2)
                                {
                                    try
                                    {
                                        var registerData = new Register
                                        {
                                            DeviceId = id,
                                            Name = dlmsObject.Name ?? "",
                                            ObjectType = dlmsObject.ObjectType,
                                            Value = value,
                                            DateEntered = DateTime.UtcNow
                                        };
                                        db.Register.Add(registerData);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to save to Register table in ReadObject");
                                    }
                                }
                            }
                            else if (dlmsObject.ObjectType == "Data")
                            {
                                if (param.AttributeId == 2)
                                {
                                    try
                                    {
                                        var dataVal = new PQM.Core.Entities.Data
                                        {
                                            DeviceId = id,
                                            Name = dlmsObject.Name ?? "",
                                            ObjectType = dlmsObject.ObjectType,
                                            Value = value,
                                            DateEntered = DateTime.UtcNow
                                        };
                                        db.Data.Add(dataVal);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to save to Data table in ReadObject");
                                    }
                                }
                            }
                            else if (string.Equals(dlmsObject.ObjectType, "IecHdlcSetup", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(dlmsObject.ObjectType, "lecHdlcSetup", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var hdlcVal = new IecHdlcSetup
                                    {
                                        DeviceId = id,
                                        Name = dlmsObject.Name ?? "",
                                        ObjectType = dlmsObject.ObjectType,
                                        Value = value,
                                        DateEntered = DateTime.UtcNow
                                    };
                                    db.IecHdlcSetup.Add(hdlcVal);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to save to IecHdlcSetup table in ReadObject");
                                }
                            }
                             else if (string.Equals(dlmsObject.ObjectType, "TcpUdpSetup", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var tcpVal = new TcpUdpSetup
                                    {
                                        DeviceId = id,
                                        Name = dlmsObject.Name ?? "",
                                        ObjectType = dlmsObject.ObjectType,
                                        Value = value,
                                        DateEntered = DateTime.UtcNow
                                    };
                                    db.TcpUdpSetup.Add(tcpVal);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to save to TcpUdpSetup table in ReadObject");
                                }
                            }
                            else if (string.Equals(dlmsObject.ObjectType, "Ip4Setup", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var ipVal = new Ip4Setup
                                    {
                                        DeviceId = id,
                                        Name = dlmsObject.Name ?? "",
                                        ObjectType = dlmsObject.ObjectType,
                                        Value = value,
                                        DateEntered = DateTime.UtcNow
                                    };
                                    db.Ip4Setup.Add(ipVal);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to save to Ip4Setup table in ReadObject");
                                }
                            }
                            else if (string.Equals(dlmsObject.ObjectType, "MacAddressSetup", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var macVal = new MacAddressSetup
                                    {
                                        DeviceId = id,
                                        Name = dlmsObject.Name ?? "",
                                        ObjectType = dlmsObject.ObjectType,
                                        Value = value,
                                        DateEntered = DateTime.UtcNow
                                    };
                                    db.MacAddressSetup.Add(macVal);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to save to MacAddressSetup table in ReadObject");
                                }
                            }
                        }
                    }

                    db.SaveChanges();
                }

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = results;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                return Error($"DLMS Error: {ex.Message}", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        // -------------------- HELPERS --------------------
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
    }
}