using CurrentWeatherData.Configuration;
using CurrentWeatherData.Middleware;
using CurrentWeatherData.Service.Interface;
using Microsoft.Win32;

var builder = WebApplication.CreateBuilder(args);

// Define a CORS policy name
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Configure CORS Services using appsettings
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // Read allowed origins from configuration
                          var allowedOrigins = builder.Configuration.GetValue<string>("CorsSettings:AllowedOrigins")?.Split(',', StringSplitOptions.RemoveEmptyEntries);

                          if (allowedOrigins != null && allowedOrigins.Length > 0)
                          {
                              policy.WithOrigins(allowedOrigins)
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          }
                      });
});

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

// Use CORS middleware
app.UseCors(MyAllowSpecificOrigins);

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