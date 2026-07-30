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
        private readonly string _connectionString;

        public ProfileController(ILogger<ProfileController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                using var db = new DataContext(_connectionString);
                var profiles = db.Profiles
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
