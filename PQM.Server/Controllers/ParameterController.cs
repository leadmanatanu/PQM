using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParameterController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ParameterController> _logger;
        private readonly DataContext _db;

        public ParameterController(ILogger<ParameterController> logger, DataContext db)
        {
            _logger = logger;
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public ActionResult Get([FromQuery] int? deviceId, [FromQuery] int? profileId, [FromQuery] int? meterTypeId)
        {
            try
            {
                var query = _db.Parameter.Where(p => p.IsVisible);

                // If deviceId is provided but meterTypeId is not, resolve meterTypeId from the device
                if (deviceId.HasValue && deviceId.Value > 0 && !meterTypeId.HasValue)
                {
                    var device = _db.Device.FirstOrDefault(d => d.Id == deviceId.Value && !d.IsDeleted);
                    if (device != null && device.MeterTypeId.HasValue)
                    {
                        meterTypeId = device.MeterTypeId.Value;
                    }
                }

                // Filter parameters by MeterTypeId
                if (meterTypeId.HasValue && meterTypeId.Value > 0)
                {
                    if (meterTypeId.Value == 1) // ABT
                    {
                        query = query.Where(p => p.MeterTypeId == 1 || p.MeterTypeId == 3 || p.MeterTypeId == null);
                    }
                    else if (meterTypeId.Value == 2) // PQ
                    {
                        query = query.Where(p => p.MeterTypeId == 2 || p.MeterTypeId == 3 || p.MeterTypeId == null);
                    }
                    // meterTypeId == 3 (Both) sees all parameters
                }

                if (profileId.HasValue && profileId.Value > 0)
                {
                    query = query.Where(p => p.ProfileId == profileId.Value);
                }

                var data = query
                    .Select(p => new
                    {
                        p.Id,
                        p.ProfileId,
                        p.MeterTypeId,
                        p.Name,
                        p.ObisCode,
                        p.Description,
                        p.Unit,
                        p.DataType,
                        p.ObjectType,
                        p.AttributeIndex,
                        p.IsHistorical,
                        p.IsVisible,
                        p.Scaler
                    })
                    .ToList();

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


    }
}
