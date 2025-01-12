using NotionReminderService.Models.NotionEvent;
using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.MessageHandler;

public interface IEventsMessageService
{
    public Task<Message> SendEventsMessageToChannel(bool isMorning);
    public Task<Message?> SendMiniReminderMessageToChannel();
}