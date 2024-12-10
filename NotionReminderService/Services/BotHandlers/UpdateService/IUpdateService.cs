using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace NotionReminderService.Services.BotHandlers.UpdateService;

public interface IUpdateService
{
    public Task HandleErrorAsync(Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken);

    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}