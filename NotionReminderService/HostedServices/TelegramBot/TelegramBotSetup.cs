using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using Telegram.Bot;

namespace NotionReminderService.HostedServices.TelegramBot;

internal class TelegramBotSetup(
    ITelegramBotClient client,
    IOptions<BotConfiguration> botConfig,
    ILogger<IHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Delete the previous webhook if it is configured.
        await client.DeleteWebhook(cancellationToken: cancellationToken).ConfigureAwait(false);
        
        var webhookUrl = botConfig.Value.BotWebhookUrl.OriginalString;

        // Replaces the previous webhook on every restart
        logger.LogInformation("Setting up the webhook");
        await client.SetWebhook(
            webhookUrl,
            secretToken: botConfig.Value.SecretToken,
            cancellationToken: cancellationToken
        )
        .ConfigureAwait(false);
        logger.LogInformation("Webhook set up successfully at {Url}", webhookUrl);

        logger.LogInformation("Setup completed successfully");
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}