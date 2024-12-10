using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.MessageService;

public interface IMessageService
{
    public Task<Message> SendMessageToChannel(bool isMorning);
}