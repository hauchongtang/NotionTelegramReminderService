using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.MessageHandler;

public interface IMessageService
{
    public Task<Message> SendMessageToChannel(bool isMorning);
}