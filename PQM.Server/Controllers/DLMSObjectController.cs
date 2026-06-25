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
                    var param2 = parameters
                        .FirstOrDefault(p => p.ObjectId == o.Id && p.AttributeId == 2);

                    var param3 = parameters
                        .FirstOrDefault(p => p.ObjectId == o.Id && p.AttributeId == 3);

                    var attr2 =
                        param2 != null && latestValues.TryGetValue(param2.Id, out var v2)
                            ? v2.Value
                            : "Waiting...";

                    var attr3 =
                        param3 == null
                            ? ""
                            : (latestValues.TryGetValue(param3.Id, out var v3) ? v3.Value : "Waiting...");

                    return new
                    {
                        o.Id,
                        o.HeaderId,
                        o.Name,
                        o.ObisCode,
                        o.ObjectType,
                        Attribute2 = attr2,
                        Attribute3 = attr3
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