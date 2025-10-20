using NotionReminderService.Models.Weather;

namespace NotionReminderService.Api.Weather;

public interface IWeatherApi
{
    public Task<List<WeatherItem>?> GetRealTimeWeather();
    public Task<RainfallResponse> GetRealTimeRainfallByLocation();
    Task<string> GetRainAreas();
}