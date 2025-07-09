using PQM.Server.Models;
using Microsoft.AspNetCore.Mvc;
using PQM.Core.IRepositories;
using PQM.Core.DomainServices;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ICSVService _csvService;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDeviceService deviceService, ICSVService csvService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _csvService = csvService;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            //var data = _deviceService.GetDevices().ToList();
            List<string> mappedParatmeter = new List<string>();
            mappedParatmeter.Add("2");
            mappedParatmeter.Add("1");
            mappedParatmeter.Add("3");
            mappedParatmeter.Add("5");
            mappedParatmeter.Add("11");
            var data = _csvService.ReadCSVData(1, "D:\\Projects\\Compac\\PQM\\PQM.Server\\CSVFiles\\data.csv", mappedParatmeter);
            data = data.OrderBy(x => x.ParameterId).ToList(); ;

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
