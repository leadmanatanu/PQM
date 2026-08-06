using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<ProfileController> _logger;
        private readonly DataContext _db;

        public ProfileController(ILogger<ProfileController> logger, DataContext db)
        {
            _logger = logger;
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                var profiles = _db.Profiles
                    .Select(p => new
                    {
                        p.ProfileId,
                        FriendlyName = string.IsNullOrWhiteSpace(p.FriendlyName) ? p.ObisCode : p.FriendlyName,
                        p.ObisCode,
                        p.Category
                    })
                    .OrderBy(p => p.FriendlyName)
                    .ToList();

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = profiles;
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
