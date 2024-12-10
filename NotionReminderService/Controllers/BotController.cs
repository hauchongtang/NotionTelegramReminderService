using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Services.BotHandlers.MessageService;
using NotionReminderService.Services.BotHandlers.UpdateService;
using NotionReminderService.Services.NotionHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NotionReminderService.Controllers;

[ApiController]
[Route("/")]
[AllowAnonymous]
public class BotController(
    ITelegramBotClient telegramBotClient,
    IUpdateService updateService,
    INotionEventParserService notionEventParserService,
    IMessageService messageService,
    ILogger<BotController> logger, 
    IOptions<BotConfiguration> botConfig)
    : ControllerBase
{
    [HttpGet("setWebhook")]
    public async Task<string> SetWebhook(CancellationToken cancellationToken)
    {
        var webhookUrl = botConfig.Value.BotWebhookUrl;
        await telegramBotClient.SetWebhook(webhookUrl.OriginalString, cancellationToken: cancellationToken);
        
        return $"Webhook set to {webhookUrl}";
    }

    [HttpGet("removeWebhook")]
    public async Task<string> RemoveWebhook(CancellationToken cancellationToken)
    {
        var webhookUrl = botConfig.Value.BotWebhookUrl;
        await telegramBotClient.DeleteWebhook(cancellationToken: cancellationToken);

        return $"Webhook {webhookUrl} removed";
    }
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Token"] != botConfig.Value.SecretToken)
            Console.WriteLine(Request.Headers["X-Telegram-Bot-Api-Token"]);
        try
        {
            await updateService.HandleUpdateAsync(update, ct);
        }
        catch (Exception exception)
        {
            await updateService.HandleErrorAsync(exception, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
        }
        return Ok();
    }

    [HttpGet("gatherEvents")]
    public async Task<IActionResult> GatherEvents(CancellationToken cancellationToken)
    {
        var events = await notionEventParserService.ParseEvent(true);
        return Ok(events);
    }

    [HttpGet("sendEventsToChannel")]
    public async Task<IActionResult> SendEventsToChannel([FromQuery] bool isMorning, CancellationToken cancellationToken)
    {
        var message = await messageService.SendMessageToChannel(isMorning);
        return Ok($"Message sent at {DateTime.Now}");
    }
}