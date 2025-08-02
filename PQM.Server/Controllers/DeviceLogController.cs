using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceLogController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IDeviceLogService _deviceLogService;
        private readonly ILogger<DeviceLogController> _logger;

        public DeviceLogController(ILogger<DeviceLogController> logger, IDeviceLogService deviceLogService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _deviceLogService = deviceLogService;
        }

        [HttpGet(Name = "GetDeviceLogs")]
        public ActionResult Get()
        {
            var data = _deviceLogService.GetDeviceLogs().ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpPost(Name = "AddDeviceLogs")]
        public ActionResult Post([FromBody] List<DeviceLog> deviceLog)
        {
            try
            {
                if (deviceLog.Count == 0)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }
                //var data = _deviceLogService.AddDeviceLogs(deviceLog);
                var data = _deviceLogService.AddBulkDeviceLogs(deviceLog);
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


        [HttpGet("Search")]
        public ActionResult Search([FromQuery] SearchParams searchParams)
        {
            var data = _deviceLogService.GetDeviceLogs(searchParams.DeviceId, searchParams.ParameterId, searchParams.PageNumber, searchParams.PageSize, searchParams.StartDate, searchParams.EndDate);
            DeviceLogSearchResult result = new DeviceLogSearchResult
            {
                DeviceLogSearch = data.Item1,
                TotalCount = data.Item2,
            };
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = result;
            return Ok(_apiResponse);
        }

    }

    public class SearchParams
    {
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
