using Microsoft.AspNetCore.Mvc;
using PQM.Server.Models;
using PQM.Infrastructure;
using PQM.Core.Entities;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DLMSObjectController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly string _connectionString;

        public DLMSObjectController(IConfiguration configuration)
        {
            _apiResponse = new APIResponse();
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found."
                );
        }

        [HttpGet("header/{headerId}")]
        public ActionResult GetObjectsByHeader(int headerId)
        {
            try
            {
                using var dbContext = new DataContext(_connectionString);

                var objects = dbContext.DLMSObject
                                       .Where(o => o.HeaderId == headerId)
                                       .ToList();

                var objectIds = objects.Select(o => o.Id).ToList();

                var parameters = dbContext.ObjectParameter
                                          .Where(p => objectIds.Contains(p.ObjectId))
                                          .ToList();

                var parameterIds = parameters.Select(p => p.Id).ToList();

                var latestValues = dbContext.ParameterValue
                    .Where(v => parameterIds.Contains(v.ParameterId))
                    .ToList()
                    .GroupBy(v => v.ParameterId)
                    .Select(g => g.OrderByDescending(v => v.Timestamp).FirstOrDefault())
                    .Where(v => v != null)
                    .ToDictionary(v => v!.ParameterId, v => v!);

                var data = objects.Select(o =>
                {
                    var objectParams = parameters.Where(p => p.ObjectId == o.Id).ToList();

                    ObjectParameter? valParam = null;
                    ObjectParameter? unitParam = null;

                    if (o.ObjectType == "Register" || o.ObjectType == "ExtendedRegister" || o.ObjectType == "DemandRegister")
                    {
                        valParam = objectParams.FirstOrDefault(p => p.AttributeId == 2);
                        unitParam = objectParams.FirstOrDefault(p => p.AttributeId == 3);
                    }
                    else
                    {
                        valParam = objectParams.FirstOrDefault();
                    }

                    var attr2 =
                        valParam != null && latestValues.TryGetValue(valParam.Id, out var v2)
                            ? v2.Value
                            : "Waiting...";

                    var attr3 =
                        unitParam == null
                            ? ""
                            : (latestValues.TryGetValue(unitParam.Id, out var v3) ? v3.Value : "Waiting...");

                    var allAttrsList = objectParams.Select(p => new
                    {
                        p.AttributeId,
                        Name = p.Name ?? "",
                        Value = latestValues.TryGetValue(p.Id, out var pv) ? pv.Value : "Waiting..."
                    }).ToList();

                    return new
                    {
                        o.Id,
                        o.HeaderId,
                        o.Name,
                        o.ObisCode,
                        o.ObjectType,
                        Attribute2 = attr2,
                        Attribute3 = attr3,
                        AllAttributes = allAttrsList
                    };
                }).ToList();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = data;

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
    }
}