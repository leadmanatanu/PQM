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

            SaveTypedReading(db, id, param.Name ?? "", param.ObjectType ?? "", value, 2);

            db.SaveChanges();

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = value;
            return Ok(_apiResponse);
        }

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

                        SaveTypedReading(db, id, dlmsObject.Name ?? "", dlmsObject.ObjectType, value, param.AttributeId);
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
