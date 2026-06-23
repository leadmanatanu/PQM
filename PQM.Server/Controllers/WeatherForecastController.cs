using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.DomainServices;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ICSVService _csvService;
        private readonly ISFTPService _sFTPService;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDeviceService deviceService, 
            ICSVService csvService, ISFTPService sFTPService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _csvService = csvService;
            _sFTPService = sFTPService;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
