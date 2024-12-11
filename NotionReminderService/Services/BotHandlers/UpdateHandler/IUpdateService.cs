using Telegram.Bot.Polling;

namespace NotionReminderService.Services.BotHandlers.UpdateHandler;

public interface IUpdateService
{
    public Task HandleErrorAsync(Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken);

    public Task HandleUpdateAsync(Telegram.Bot.Types.Update update, CancellationToken cancellationToken);
}