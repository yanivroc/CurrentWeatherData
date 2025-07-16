using CurrentWeatherData.Configuration;
using CurrentWeatherData.Service.Interface;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CurrentWeatherData.Service
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;
        private readonly WeatherSettings _weatherSettings;
        private int _currentApiKeyIndex = 0;

        public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger, IOptions<WeatherSettings> options)
        {
            _httpClient = httpClientFactory.CreateClient("OpenWeatherMapClient");
            _logger = logger;
            _weatherSettings = options.Value;
        }

        public async Task<string?> GetWeatherDescriptionAsync(string city, string country)
        {
            // Use the BaseUrl property from the injected _weatherSettings object
            if (string.IsNullOrEmpty(_weatherSettings.BaseUrl))
            {
                _logger.LogError("OpenWeatherMap BaseUrl is not configured.");
                return null;
            }

            // Check for valid API keys
            if (_weatherSettings.OpenWeatherMapApiKeys.Length == 0)
            {
                _logger.LogError("OpenWeatherMap API keys are not configured.");
                return null;
            }

            // Get the current OpenWeatherMap API key and cycle to the next one.
            string apiKey = _weatherSettings.OpenWeatherMapApiKeys[_currentApiKeyIndex];
            _currentApiKeyIndex = (_currentApiKeyIndex + 1) % _weatherSettings.OpenWeatherMapApiKeys.Length;

            // Construct the API URL.
            string requestUrl = $"{_weatherSettings.BaseUrl}weather?q={city},{country}&appid={apiKey}";

            try
            {
                _logger.LogInformation($"Calling OpenWeatherMap API: {requestUrl}");
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                // Check if the response was successful (HTTP 2xx status code).
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"OpenWeatherMap raw response: {jsonResponse}");

                    // Parse the JSON response.
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        // Navigate to the "weather" array and get the first element's "description".
                        if (doc.RootElement.TryGetProperty("weather", out JsonElement weatherArray) &&
                            weatherArray.ValueKind == JsonValueKind.Array &&
                            weatherArray.EnumerateArray().Any())
                        {
                            var firstWeather = weatherArray.EnumerateArray().First();
                            if (firstWeather.TryGetProperty("description", out JsonElement descriptionElement))
                            {
                                return descriptionElement.GetString();
                            }
                        }
                    }
                    _logger.LogWarning($"'description' field not found in OpenWeatherMap response for {city}, {country}.");
                    return null; // Description field not found
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"OpenWeatherMap returned 404 for {city}, {country}.");
                    return null; // City/country not found by OpenWeatherMap
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenWeatherMap API error for {city},{country}: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"OpenWeatherMap API returned status code {response.StatusCode}: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Network error or HTTP issue when calling OpenWeatherMap for {city}, {country}.");
                throw; // Re-throw to be handled by the controller
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error from OpenWeatherMap response for {city}, {country}.");
                throw new Exception("Failed to parse OpenWeatherMap response.", ex); // Wrap and re-throw
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred in WeatherService for {city}, {country}.");
                throw; // Re-throw for generic handling
            }
        }
    }
}