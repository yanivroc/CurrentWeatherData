namespace CurrentWeatherData.Configuration
{
    public class WeatherSettings
    {
        // Base URL from configuration
        public string? BaseUrl { get; set; }
        // OpenWeatherMap API keys
        public string[] OpenWeatherMapApiKeys { get; set; } = Array.Empty<string>();

        public WeatherSettings() { }
    }
}
