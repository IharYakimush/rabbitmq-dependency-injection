using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using RabbitMQ.DependencyInjection;

namespace SampleWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IRabbitMqModel<WeatherForecast> _rabbitMqModel;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRabbitMqModel<WeatherForecast> rabbitMqModel)
        {
            _logger = logger;
            _rabbitMqModel = rabbitMqModel ?? throw new ArgumentNullException(nameof(rabbitMqModel));
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            return Enumerable.Range(1, 5).Select(index =>
            {
                string value = Summaries[rng.Next(Summaries.Length)];

                this._rabbitMqModel.Model.BasicPublish("myExc", "routingKey", false, null, Encoding.UTF8.GetBytes(value));

                return new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = value
                };
            }).ToArray();
        }
    }
}
