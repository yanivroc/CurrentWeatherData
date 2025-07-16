using CurrentWeatherData.Configuration;
using CurrentWeatherData.Middleware;
using CurrentWeatherData.Service.Interface;
using Microsoft.Win32;

var builder = WebApplication.CreateBuilder(args);

// Configure IOptions<WeatherSettings> by binding the "Weather" section
builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection("WeatherSettings"));

// Configure IOptions<RateLimitSettings>
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));

// Add IHttpClientFactory services with a named client
builder.Services.AddHttpClient("OpenWeatherMapClient");

// Register WeatherService as a Singleton for dependency injection
// The DI container will automatically provide IHttpClientFactory, ILogger<WeatherService>,
// and IOptions<WeatherSettings> to its constructor
builder.Services.AddSingleton<IWeatherService, CurrentWeatherData.Service.WeatherService>();

// Register RateLimitMiddleware and TimeProvider
builder.Services.AddTransient<RateLimitMiddleware>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// Add services to the container.
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Use the developer exception page for detailed error information in development
    app.UseDeveloperExceptionPage();
}

// Redirect HTTP requests to HTTPS for security
app.UseHttpsRedirection();

// Use rate limit middleware
app.UseMiddleware<RateLimitMiddleware>();

// Enable authorization middleware
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Run the application
app.Run();