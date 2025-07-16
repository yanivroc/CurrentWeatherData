using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace CurrentWeatherData.Tests;

// Inherit from WebApplicationFactory to create a test host
public class RateLimitMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // A. Test for a valid API key
    [Fact]
    public async Task Middleware_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var validApiKey = "test-api-key-123";

        // Act
        client.DefaultRequestHeaders.Add("X-Api-Key", validApiKey);
        var response = await client.GetAsync("/weather"); // Assuming this is your endpoint

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // B. Test for a missing API key
    [Fact]
    public async Task Middleware_WithoutApiKey_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/weather");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // C. Test for an invalid API key
    [Fact]
    public async Task Middleware_WithInvalidApiKey_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidApiKey = "invalid-key";

        // Act
        client.DefaultRequestHeaders.Add("X-Api-Key", invalidApiKey);
        var response = await client.GetAsync("/weather");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // D. Test for exceeding the rate limit
    [Fact]
    public async Task Middleware_ExceedsRateLimit_Returns429TooManyRequests()
    {
        // Arrange
        var client = _factory.CreateClient();
        var validApiKey = "test-api-key-123";

        // Act & Assert - Make requests up to the limit
        client.DefaultRequestHeaders.Add("X-Api-Key", validApiKey);
        for (int i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/weather");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Act - Make one more request to exceed the limit
        var overLimitResponse = await client.GetAsync("/weather");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, overLimitResponse.StatusCode);
        Assert.True(overLimitResponse.Headers.Contains("Retry-After"));
    }
}