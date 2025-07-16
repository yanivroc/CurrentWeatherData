using CurrentWeatherData.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CurrentWeatherData.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;

        // Constructor for dependency injection.
        // The WeatherService and Logger are injected by the ASP.NET Core runtime.
        public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        // <summary>
        /// Retrieves weather description for a given city and country.
        /// </summary>
        /// <param name="city">The name of the city.</param>
        /// <param name="country">The country code (e.g., "us", "uk").</param>
        /// <returns>The weather description or an error message.</returns>
        [HttpGet] // Specifies that this action responds to HTTP GET requests.
        public async Task<IActionResult> GetWeather([FromQuery] string city, [FromQuery] string country)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
            {
                _logger.LogWarning("Invalid request: City or country name is missing.");
                return BadRequest("City and country name are required.");
            }

            try
            {
                // Call the weather service to get the weather description.
                // The rate limiting is handled by the middleware, so we don't need to check it here.
                var weatherDescription = await _weatherService.GetWeatherDescriptionAsync(city, country);

                if (weatherDescription == null)
                {
                    _logger.LogInformation($"Weather data not found for {city}, {country}.");
                    return NotFound($"Weather data not found for {city}, {country}. Please check the city and country name.");
                }

                _logger.LogInformation($"Successfully retrieved weather for {city}, {country}: {weatherDescription}");
                return Ok(new { description = weatherDescription }); // Return the description in a JSON object.
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error calling OpenWeatherMap API for {city}, {country}.");
                return StatusCode(500, "Error retrieving weather data from external service. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while processing weather request for {city}, {country}.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
