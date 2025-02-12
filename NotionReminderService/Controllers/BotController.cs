using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionReminderService.Services.BotHandlers.MessageHandler;
using NotionReminderService.Services.BotHandlers.UpdateHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Services.NotionHandlers.NotionEventUpdater;
using NotionReminderService.Utils;
using Telegram.Bot.Types;

namespace NotionReminderService.Controllers;

[ApiController]
[Route("/")]
[AllowAnonymous]
public class BotController(
    IUpdateService updateService,
    IEventsMessageService eventsMessageService,
    IWeatherMessageService weatherMessageService,
    INotionEventUpdaterService notionEventUpdaterService,
    IDateTimeProvider dateTimeProvider,
    ILogger<BotController> logger)
    : ControllerBase
{
    [HttpPost("getUpdates")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
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

    [HttpGet("sendEventsToChannel")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> SendEventsToChannel([FromQuery] bool isMorning, CancellationToken cancellationToken)
    {
        var message = await eventsMessageService.SendEventsMessageToChannel(isMorning);
        return Ok($"Message sent at {dateTimeProvider.Now}");
    }

    [HttpGet("sendEventRemindersToChannel")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> SendRemindersToChannel(CancellationToken cancellationToken)
    {
        var message = await eventsMessageService.SendMiniReminderMessageToChannel();
        return Ok(message is null ? "No reminders to send." : message.Text);
    }

    [HttpGet("sendWeatherSummaryToChannel")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> SendWeatherSummaryToChannel(CancellationToken cancellationToken)
    {
        await weatherMessageService.SendMessage(null);
        return Ok();
    }

    [HttpGet("sendReminderForUnhandledEvents")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> SendReminderForUnhandledEvents(CancellationToken cancellationToken)
    {
        await eventsMessageService.SendReminderForUnhandledEvents();
        return Ok();
    }

    [HttpPatch("updateEventsToCompleted")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateEventsToCompleted(CancellationToken cancellationToken)
    {
        var updatedPages = await notionEventUpdaterService.UpdateEventsToCompleted();
        return Ok($"{updatedPages.Count} events updated to 'Completed'");
    }

    [HttpPatch("updateEventsToInProgress")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateEventsToInProgress([FromQuery] bool isMorning,
        CancellationToken cancellationToken)
    {
        var updatedPages = await notionEventUpdaterService.UpdateEventsToInProgress(isMorning);
        return Ok($"{updatedPages.Count} events updated to 'In progress'");
    }

    [HttpPatch("updateCancelledEventsToTrash")]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateCancelledEventsToTrash(CancellationToken cancellationToken)
    {
        var deletedPages = await notionEventUpdaterService.UpdateEventsToTrash();
        return Ok($"{deletedPages.Count} cancellation events deleted.");
    }
}