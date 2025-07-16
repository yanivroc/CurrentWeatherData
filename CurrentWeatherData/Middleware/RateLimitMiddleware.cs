using CurrentWeatherData.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CurrentWeatherData.Middleware
{
    public class RateLimitMiddleware : IMiddleware
    {
        private readonly ILogger<RateLimitMiddleware> _logger;

        // Inject a time provider
        private readonly TimeProvider _timeProvider;

        // Custom API keys for your service
        private readonly HashSet<string> _validApiKeys;

        // Stores request timestamps for each API key
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTimestamps = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();

        private const int MaxRequestsPerHour = 5;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

        public RateLimitMiddleware(ILogger<RateLimitMiddleware> logger, IOptions<RateLimitSettings> options, TimeProvider timeProvider)
        {
            _logger = logger;
            _timeProvider = timeProvider;
            _validApiKeys = options.Value.ValidApiKeys;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            string? apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("API Key missing from request.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key is missing. Please provide a valid 'X-Api-Key' header.");
                return;
            }

            if (!_validApiKeys.Contains(apiKey))
            {
                _logger.LogWarning($"Invalid API Key received: {apiKey}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            ConcurrentQueue<DateTime> timestamps = _requestTimestamps.GetOrAdd(apiKey, _ => new ConcurrentQueue<DateTime>());

            // Use _timeProvider.GetUtcNow() instead of DateTime.UtcNow
            DateTimeOffset now = _timeProvider.GetUtcNow();
            while (timestamps.TryPeek(out DateTime oldestTimestamp) && (now - oldestTimestamp) > RateLimitWindow)
            {
                timestamps.TryDequeue(out _);
            }

            if (timestamps.Count >= MaxRequestsPerHour)
            {
                _logger.LogWarning($"Rate limit exceeded for API Key: {apiKey}");
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = RateLimitWindow.TotalSeconds.ToString();
                await context.Response.WriteAsync("Hourly rate limit exceeded. Please try again later.");
                return;
            }

            // Use now.UtcDateTime to enqueue the timestamp
            timestamps.Enqueue(now.UtcDateTime);
            _logger.LogInformation($"API Key {apiKey} request count: {timestamps.Count}/{MaxRequestsPerHour}");

            await next(context);
        }
    }
}