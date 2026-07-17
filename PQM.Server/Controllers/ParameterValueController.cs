using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/devicelog")]
    public class ParameterValueController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ParameterValueController> _logger;
        private readonly string _connectionString;

        public ParameterValueController(ILogger<ParameterValueController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get()
        {
            using var db = new DataContext(_connectionString);
            var data = db.ParameterValue.OrderByDescending(x => x.Timestamp).Take(100).ToList();
            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = data;
            return Ok(_apiResponse);
        }

        [HttpPost]
        public ActionResult Post([FromBody] List<ParameterValue> values)
        {
            try
            {
                if (values == null || values.Count == 0)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
                    return Ok(_apiResponse);
                }

                using var db = new DataContext(_connectionString);
                db.ParameterValue.AddRange(values);
                db.SaveChanges();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = values.Count;
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
            try
            {
                using var db = new DataContext(_connectionString);
                var query = from pv in db.ParameterValue
                            join d in db.Device on pv.DeviceId equals d.Id
                            join p in db.Parameter on pv.ParameterId equals p.Id
                            where pv.DeviceId == searchParams.DeviceId
                            select new ParameterValueSearch
                            {
                                Id = pv.Id,
                                Value = pv.Value ?? "",
                                DateStamp = pv.Timestamp,
                                DeviceName = d.Name,
                                ParameterName = p.Name,
                                ParameterId = pv.ParameterId
                            };

                if (searchParams.ParameterId > 0)
                {
                    query = query.Where(q => q.ParameterId == searchParams.ParameterId);
                }

                if (searchParams.StartDate != default)
                {
                    query = query.Where(q => q.DateStamp >= searchParams.StartDate);
                }

                if (searchParams.EndDate != default)
                {
                    var endDate = searchParams.EndDate.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(q => q.DateStamp <= endDate);
                }

                var totalCount = query.Count();
                var items = query.OrderByDescending(q => q.DateStamp)
                                 .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
                                 .Take(searchParams.PageSize)
                                 .ToList();

                var result = new ParameterValueSearchResult
                {
                    DeviceLogSearch = items,
                    TotalCount = totalCount
                };

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = result;
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

    public class ParameterValueSearch
    {
        public long Id { get; set; }
        public required string Value { get; set; }
        public DateTime DateStamp { get; set; }
        public required string DeviceName { get; set; }
        public required string ParameterName { get; set; }
        public int ParameterId { get; set; }
    }

    public class ParameterValueSearchResult
    {
        public int TotalCount { get; set; }
        public List<ParameterValueSearch> DeviceLogSearch { get; set; } = new();
    }
}
