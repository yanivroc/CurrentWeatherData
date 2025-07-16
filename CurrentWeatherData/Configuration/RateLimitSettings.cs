namespace CurrentWeatherData.Configuration
{
    public class RateLimitSettings
    {
        // OpenWeatherMap API keys
        public HashSet<string> ValidApiKeys { get; set; } = new HashSet<string>();

        public RateLimitSettings() { }
    }
}
