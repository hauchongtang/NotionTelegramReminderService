namespace NotionReminderService.Services.BotHandlers.WeatherHandler;

public interface IWeatherMessageService
{
    public Task SendMessage(string? location);
}