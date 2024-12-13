using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.WeatherHandler;

public interface IWeatherMessageService
{
    public Task SendMessage(Chat? chat);
}