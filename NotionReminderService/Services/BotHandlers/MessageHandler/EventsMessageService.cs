using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Services.NotionHandlers.NotionEventRetrival;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotionReminderService.Services.BotHandlers.MessageHandler;

public class EventsMessageService(INotionEventRetrivalService notionEventRetrivalService, ITelegramBotClient telegramBotClient,
    IDateTimeProvider dateTimeProvider, IOptions<BotConfiguration> botConfig, IOptions<NotionConfiguration> notionConfig, 
    ILogger<IEventsMessageService> logger)
    : IEventsMessageService
{
    public async Task<Message> SendEventsMessageToChannel(bool isMorning)
    {
        var events = await notionEventRetrivalService.GetNotionEvents(isMorning);
        var ongoingEvents = await notionEventRetrivalService.GetOngoingEvents();
        var greetings = isMorning ? "Morning" : "Evening";
        var messageBody = $"""
                           <b>📅 Plans! Overview</b> | <b>{events.Count} Event(s) upcoming</b> | Today → {dateTimeProvider.Now.AddDays(3):dddd}
                           <b><i>Good {greetings}, there are {events.Count} events upcoming in the next 3 days.</i></b>
                           --------------------------
                           
                           """;
        
        foreach (var notionEvent in events)
        {
            var eventMessageFormat = new NotionEventMessageBuilder()
                .WithNotionEvent(notionEvent, dateTimeProvider.Now)
                .Build();
            messageBody += eventMessageFormat;
        }
        
        messageBody += $"""
                        
                        <b><i>Ongoing Events: </i></b>
                        --------------------------
                        """;

        foreach (var notionEvent in ongoingEvents)
        {
            var eventMessageFormat = new NotionEventMessageBuilder()
                .WithNotionEvent(notionEvent, dateTimeProvider.Now)
                .Build();
            messageBody += eventMessageFormat;
        }

        messageBody += $"""
                        
                        Updated as of: {dateTimeProvider.Now:F}
                        """;

        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithUrl("View on Notion",
                $"https://www.notion.so/{notionConfig.Value.DatabaseId}"))
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData("Retrieve current ☁️ forecast", "triggerForecast"));

        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody,
            ParseMode.Html, replyMarkup: replyMarkup);
        return message;
    }

    public async Task<Message?> SendMiniReminderMessageToChannel()
    {
        var events = await notionEventRetrivalService.GetMiniReminders();
        if (events.Count == 0) return null;
        
        var messageBody = $"""
                           <b>⚠️ Reminders</b> | <b>{events.Count} Reminders(s) Today</b>
                           --------------------------

                           """;
        foreach (var notionEvent in events)
        {
            if (notionEvent.MiniReminderDesc is null || notionEvent.ReminderPeriod is null) continue;
            
            var formattedDate = NotionEventDateFormatter.FormatEventDate(notionEvent, dateTimeProvider.Now);
            messageBody += $"""

                            For <b>🌟 <a href="{notionEvent.Url}">{notionEvent.Name}</a></b>, 
                            that is happening on {formattedDate},
                            please be reminded to: {notionEvent.MiniReminderDesc}.

                            """;
        }

        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody,
            ParseMode.Html);
        return message;
    }

    public async Task<Message?> SendReminderForUnhandledEvents() {
        var events = await notionEventRetrivalService.GetUnhandledEvents();
        if (events.Count == 0) return null;

        var messageBody = $"""
                           <b>⚠️ All Unhandled Events </b> | <b>{events.Count} Events(s)</b>
                           --------------------------
                           """;
        foreach (var notionEvent in events) {
            if (notionEvent.Status == "Rescheduled") {
                // Do something
            }

            if (notionEvent.Status == "90 % Done") {
                // Do something
            }

            if (notionEvent.Status == "KIV") {
                // Do something
            }
        }
        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody, ParseMode.Html);
        return message;
    }
}