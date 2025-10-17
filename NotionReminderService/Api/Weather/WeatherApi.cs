using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Models.Weather;
using NotionReminderService.Utils;

namespace NotionReminderService.Api.Weather;

public class WeatherApi(IHttpClientFactory httpClientFactory, IDateTimeProvider dateTimeProvider,
    IOptions<WeatherConfiguration> weatherConfig) : IWeatherApi
{
    public async Task<List<WeatherItem>?> GetRealTimeWeather()
    {
        var weatherData = await GetCurrentWeatherData();
        return weatherData?.Data.Items;
    }
    
    private async Task<RealTimeWeatherResponse?> GetCurrentWeatherData()
    {
        using HttpClient httpClient = httpClientFactory.CreateClient();
        var dateNow = $"{dateTimeProvider.Now:yyyy-MM-dd}T{dateTimeProvider.Now:HH:mm:ss}";
        var response =
            await httpClient.GetAsync(
                new Uri($"{weatherConfig.Value.DataGovUrl}/real-time/api/two-hr-forecast?date={dateNow}"));
        response.EnsureSuccessStatusCode();

        return await ResponseHandler.HandleResponse<RealTimeWeatherResponse>(response);
    }

    public async Task<RainfallResponse> GetRealTimeRainfallByLocation()
    {
        using HttpClient httpClient = httpClientFactory.CreateClient();
        var dateNow = $"{dateTimeProvider.Now:yyyy-MM-dd}T{dateTimeProvider.Now:HH:mm:ss}";
        var response =
            await httpClient.GetAsync(
                new Uri($"{weatherConfig.Value.DataGovUrl}/real-time/api/rainfall?date={dateNow}"));
        response.EnsureSuccessStatusCode();
        
        return await ResponseHandler.HandleResponse<RainfallResponse>(response);
    }
}