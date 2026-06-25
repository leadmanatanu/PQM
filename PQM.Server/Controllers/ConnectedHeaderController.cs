using Microsoft.AspNetCore.Mvc;
using PQM.Server.Models;
using PQM.Infrastructure;
using PQM.Core.Entities;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnectedHeaderController : ControllerBase
    {
        private readonly APIResponse _apiResponse;
        private readonly string _connectionString;

        public ConnectedHeaderController(IConfiguration configuration)
        {
            _apiResponse = new APIResponse();
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found."
                );
        }

        [HttpGet("device/{deviceId}")]
        public ActionResult GetHeadersByDevice(int deviceId)
        {
            try
            {
                using var dbContext = new DataContext(_connectionString);

                var data = dbContext.ConnectedHeader
                                    .Where(h => h.DeviceId == deviceId)
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
                _apiResponse.Errors = new List<string> { ex.Message };

                return Ok(_apiResponse);
            }
        }
    }
}