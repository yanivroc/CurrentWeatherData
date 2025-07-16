using CurrentWeatherData.Configuration;
using CurrentWeatherData.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace CurrentWeatherData.Tests
{
    public class WeatherServiceTests
    {
        private readonly ILogger<WeatherService> _mockLogger;
        private readonly IOptions<WeatherSettings> _loadedOptions; // Changed name to reflect loaded config

        public WeatherServiceTests()
        {
            // Mock the logger to prevent console output during tests
            _mockLogger = new Mock<ILogger<WeatherService>>().Object;

            // --- NEW: Load WeatherSettings from appsettings.tests.json ---
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set base path to where test assembly runs
                .AddJsonFile("appsettings.tests.json", optional: false, reloadOnChange: false) // Load the test-specific config
                .Build();

            // Create a WeatherSettings instance and bind the "Weather" section to it
            var weatherSettings = new WeatherSettings();
            configuration.GetSection("Weather").Bind(weatherSettings);

            // Create the IOptions<WeatherSettings> instance from the loaded settings
            _loadedOptions = Options.Create(weatherSettings);
            // --- END NEW ---
        }

        [Fact]
        public async Task GetWeatherDescriptionAsync_ValidResponse_ReturnsDescription()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var mockResponseJson = "{\"weather\":[{\"description\":\"clear sky\"}]}";

            // Match any request to the base URL
            mockHttp.When("http://api.openweathermap.org/data/2.5/weather*")
                    .Respond("application/json", mockResponseJson);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient("OpenWeatherMapClient"))
                                 .Returns(new HttpClient(mockHttp));

            // Use the _loadedOptions which now comes from appsettings.tests.json
            var service = new WeatherService(mockHttpClientFactory.Object, _mockLogger, _loadedOptions);

            // Act
            var result = await service.GetWeatherDescriptionAsync("test-city", "us");

            // Assert
            Assert.Equal("clear sky", result);
        }

        [Fact]
        public async Task GetWeatherDescriptionAsync_CityNotFound_ReturnsNull()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();

            // Match any request to the base URL and respond with 404
            mockHttp.When("http://api.openweathermap.org/data/2.5/weather*")
                    .Respond(HttpStatusCode.NotFound); // Simulate a 404 response

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient("OpenWeatherMapClient"))
                                 .Returns(new HttpClient(mockHttp));

            // Use the _loadedOptions which now comes from appsettings.tests.json
            var service = new WeatherService(mockHttpClientFactory.Object, _mockLogger, _loadedOptions);

            // Act
            var result = await service.GetWeatherDescriptionAsync("non-existent-city", "us");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetWeatherDescriptionAsync_MissingDescription_ReturnsNull()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var mockResponseJson = "{\"weather\":[{\"main\":\"Clouds\"}]}"; // Missing 'description'

            // Match any request to the base URL
            mockHttp.When("http://api.openweathermap.org/data/2.5/weather*")
                    .Respond("application/json", mockResponseJson);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient("OpenWeatherMapClient"))
                                 .Returns(new HttpClient(mockHttp));

            // Use the _loadedOptions which now comes from appsettings.tests.json
            var service = new WeatherService(mockHttpClientFactory.Object, _mockLogger, _loadedOptions);

            // Act
            var result = await service.GetWeatherDescriptionAsync("test-city", "us");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetWeatherDescriptionAsync_InvalidJson_ThrowsException()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var mockResponseJson = "{ This is invalid JSON }";

            // Match any request to the base URL
            mockHttp.When("http://api.openweathermap.org/data/2.5/weather*")
                    .Respond("application/json", mockResponseJson);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient("OpenWeatherMapClient"))
                                 .Returns(new HttpClient(mockHttp));

            // Use the _loadedOptions which now comes from appsettings.tests.json
            var service = new WeatherService(mockHttpClientFactory.Object, _mockLogger, _loadedOptions);

            // Assert
            // EXPECT System.Exception because WeatherService wraps JsonException in a generic Exception
            await Assert.ThrowsAsync<Exception>(() => service.GetWeatherDescriptionAsync("test-city", "us"));
        }

        [Fact]
        public async Task GetWeatherDescriptionAsync_UsesConfiguredApiKey()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var expectedApiKey = _loadedOptions.Value.OpenWeatherMapApiKeys.First(); // Get the key from loaded config
            var mockResponseJson = "{\"weather\":[{\"description\":\"sunny\"}]}";

            // Use WithQueryString for more precise matching of parameters
            mockHttp.When("http://api.openweathermap.org/data/2.5/weather")
                    .WithQueryString("q", "any-city,any-country") // Match the city and country parameter
                    .WithQueryString("appid", expectedApiKey) // Match the API key parameter
                    .Respond("application/json", mockResponseJson);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient("OpenWeatherMapClient"))
                                 .Returns(new HttpClient(mockHttp));

            var service = new WeatherService(mockHttpClientFactory.Object, _mockLogger, _loadedOptions);

            // Act
            var result = await service.GetWeatherDescriptionAsync("any-city", "any-country");

            // Assert
            Assert.Equal("sunny", result); // Verify the call was made and processed
            mockHttp.VerifyNoOutstandingRequest(); // Ensure the expected request was made and consumed
        }
    }
}