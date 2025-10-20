using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Models.Weather;
using NotionReminderService.Utils;
using PuppeteerSharp;

namespace NotionReminderService.Api.Weather;

public class WeatherApi(IHttpClientFactory httpClientFactory, IDateTimeProvider dateTimeProvider,
    IOptions<WeatherConfiguration> weatherConfig, IOptions<BrowserConfiguration> browserConfig) : IWeatherApi
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

    public async Task<string> GetRainAreas()
    {
        var options = new ConnectOptions
        {
            BrowserWSEndpoint = $"wss://{browserConfig.Value.BrowserWSEndpoint}?token={browserConfig.Value.Token}"
        };
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.ConnectAsync(options);
        var page = await browser.NewPageAsync();
        await page.GoToAsync("https://www.nea.gov.sg/weather/rain-areas");
        var notificationBtn = await page.QuerySelectorAsync(".notification-btn-close");
        await notificationBtn.ClickAsync();
        var checkboxMrtStn = await page.QuerySelectorAsync("#checkbox-mrt");
        await checkboxMrtStn.ClickAsync();
        var elementHandle = await page.QuerySelectorAsync(".forecast-widget__tab");
        await elementHandle.ScrollIntoViewAsync();

        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 800,
            Height = 900,
            DeviceScaleFactor = 2
        });
        await page.ScreenshotAsync("rain-areas.png");
        await browser.CloseAsync();
        return "rain-areas.png";
    }
}