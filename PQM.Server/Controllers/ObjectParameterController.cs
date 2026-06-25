using Microsoft.AspNetCore.Mvc;
using PQM.Server.Models;
using PQM.Infrastructure;
using PQM.Core.Entities;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObjectParameterController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly IConfiguration _configuration;

        public ObjectParameterController(IConfiguration configuration)
        {
            _apiResponse = new APIResponse();
            _configuration = configuration;
        }

        [HttpGet("object/{objectId}")]
        public ActionResult GetParametersByObject(int objectId)
        {
            try
            {
                var connStr = _configuration.GetValue<string>("ConnectionString");

                if (string.IsNullOrWhiteSpace(connStr))
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                    _apiResponse.Errors = new List<string>
                    {
                        "Connection string is missing."
                    };
                    return Ok(_apiResponse);
                }

                using (var dbContext = new DataContext(connStr))
                {
                    var parameters = dbContext.ObjectParameter
                        .Where(p => p.ObjectId == objectId)
                        .ToList();

                    var paramIds = parameters.Select(p => p.Id).ToList();

                    var latestValues = dbContext.ParameterValue
                        .Where(v => paramIds.Contains(v.ParameterId))
                        .ToList()
                        .GroupBy(v => v.ParameterId)
                        .Select(g => g.OrderByDescending(v => v.Timestamp).First())
                        .ToDictionary(v => v.ParameterId, v => v);

                    var data = parameters.Select(p => new
                    {
                        p.Id,
                        p.ObjectId,
                        p.AttributeId,
                        p.Name,
                        p.DataType,
                        p.AccessType,
                        Value = latestValues.ContainsKey(p.Id)
                            ? latestValues[p.Id].Value
                            : "Waiting...",
                        Timestamp = latestValues.ContainsKey(p.Id)
                            ? latestValues[p.Id].Timestamp.ToString("g")
                            : ""
                    }).ToList();

                    _apiResponse.Status = true;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    _apiResponse.Data = data;
                }

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