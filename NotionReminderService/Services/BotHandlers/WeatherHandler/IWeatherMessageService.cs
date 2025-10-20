using NotionReminderService.Models.Weather.Rainfall;
using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.WeatherHandler;

public interface IWeatherMessageService
{
    public Task SendMessage(Chat? chat);
    Task UpdateRainfallStations();
    Task<string?> CreateCurrentDayRainfallIfNotExists();
    Task UpdateRainfallReadings();
    Task<RainfallSummary?> GetRainfallSummaryLastHour();
    Task SendRainfallSummaryMessage(Chat? chat);
    Task DownloadAndSendRainAreasImage(Chat? chat);
}