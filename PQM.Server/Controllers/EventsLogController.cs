using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.Entities;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsLogController : ControllerBase
    {
        public APIResponse _apiResponse;
        private readonly IEventLogService _eventLogService;
        private readonly ILogger<EventsLogController> _logger;

        public EventsLogController(ILogger<EventsLogController> logger, IEventLogService eventLogService)
        {
            _apiResponse = new APIResponse();
            _logger = logger;
            _eventLogService = eventLogService;
        }

        [HttpGet(Name = "GetEventLogs")]
        public ActionResult Get()
        {
            var data = _eventLogService.GetEventLogs().ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpGet("Search")]
        public ActionResult Search([FromQuery] SearchParams searchParams)
        {
            var data = _eventLogService.GetEventLogs(searchParams.DeviceId, searchParams.EventType, searchParams.PageNumber, searchParams.PageSize, searchParams.StartDate, searchParams.EndDate);
            EventLogSearchResult result = new EventLogSearchResult
            {
                EventLogSearch = data.Item1,
                TotalCount = data.Item2,
            };
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = result;
            return Ok(_apiResponse);
        }

    }
}
