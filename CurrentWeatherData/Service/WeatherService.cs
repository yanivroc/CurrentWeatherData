using CurrentWeatherData.Service.Interface;

namespace CurrentWeatherData.Service
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;
        public WeatherService() 
        { 
        
        }

        public Task<string?> GetWeatherDescriptionAsync(string city, string country)
        {
            throw new NotImplementedException();
        }
    }
}
