namespace CurrentWeatherData.Service.Interface
{
    public interface IWeatherService
    {
        Task<string?> GetWeatherDescriptionAsync(string city, string country);
    }
}
