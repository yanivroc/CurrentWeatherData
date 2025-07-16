namespace CurrentWeatherData.Configuration
{
    public class WeatherSettings
    {
        // Public property to hold the Base URL from configuration
        public string? BaseUrl { get; set; }
        // OpenWeatherMap API keys
        public string[] OpenWeatherMapApiKeys { get; set; } = Array.Empty<string>();

        // The DI binding process requires this parameterless constructor.
        public WeatherSettings() { }
    }
}
