using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.NotionHandlers.NotionEventParser;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotionReminderService.Services.BotHandlers.MessageHandler;

public class EventsMessageService(INotionEventParserService notionEventParserService, ITelegramBotClient telegramBotClient,
    IDateTimeProvider dateTimeProvider, IOptions<BotConfiguration> botConfig, IOptions<NotionConfiguration> notionConfig, 
    ILogger<IEventsMessageService> logger)
    : IEventsMessageService
{
    public async Task<Message> SendEventsMessageToChannel(bool isMorning)
    {
        var events = await notionEventParserService.ParseEvent(isMorning);
        var ongoingEvents = await notionEventParserService.GetOngoingEvents();
        var greetings = isMorning ? "Morning" : "Evening";
        var messageBody = $"""
                           <b>📅 Plans! Overview</b> | <b>{events.Count} Event(s) upcoming</b> | Today → {dateTimeProvider.Now.AddDays(3):dddd}
                           <b><i>Good {greetings}, there are {events.Count} events upcoming in the next 3 days.</i></b>
                           --------------------------
                           
                           """;
        
        foreach (var notionEvent in events)
        {
            var eventDate = FormatEventDate(notionEvent);
            var eventMessageFormat = $"""

                                      <b>🌟 <a href="{notionEvent.Url}">{notionEvent.Name}</a></b>
                                      <b>📍 {notionEvent.Where}</b>
                                      <b>👥 {notionEvent.Person}</b>
                                      <b>▶️ {notionEvent.Status}</b>
                                      <b>🏷️ {notionEvent.Tags}</b>
                                      <b>📅 {eventDate}</b>

                                      """;
            messageBody += eventMessageFormat;
        }
        
        messageBody += $"""
                        
                        <b><i>Ongoing Events: </i></b>
                        --------------------------
                        """;

        foreach (var notionEvent in ongoingEvents)
        {
            var eventDate = FormatEventDate(notionEvent);
            var eventMessageFormat = $"""
                                      
                                      <b>🌟 <a href="{notionEvent.Url}">{notionEvent.Name}</a></b>
                                      <b>📍 {notionEvent.Where}</b>
                                      <b>👥 {notionEvent.Person}</b>
                                      <b>▶️ {notionEvent.Status}</b>
                                      <b>🏷️ {notionEvent.Tags}</b>
                                      <b>📅 {eventDate}</b>
                                      
                                      """;
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

    public string FormatEventDate(NotionEvent notionEvent)
    {
        if (notionEvent.Start is null) return string.Empty;
        
        string eventDate;
        if (EventIsToday(notionEvent))
        {
            eventDate = notionEvent.End is null 
                ? notionEvent.IsWholeDayEvent 
                    ? "Today" 
                    : $"Today @ {notionEvent.Start.Value:t}" 
                : notionEvent.IsWholeDayEvent 
                    ? $"Today \u2192 {notionEvent.End.Value:F}" 
                    : $"Today @ {notionEvent.Start.Value:t} \u2192 {notionEvent.End.Value:F}";
        }
        else
        {
            eventDate = notionEvent.End is null
                ? notionEvent.IsWholeDayEvent 
                    ? $"{notionEvent.Start.Value:D}" 
                    : $"{notionEvent.Start.Value:F}"
                : notionEvent.IsWholeDayEvent 
                    ? $"{notionEvent.Start.Value:D} \u2192 {notionEvent.End.Value:D}"
                    : $"{notionEvent.Start.Value:F} \u2192 {notionEvent.End.Value:F}";
        }

        return eventDate;
    }

    private bool EventIsToday(NotionEvent notionEvent)
    {
        return notionEvent.Start != null
               && notionEvent.Start.Value.Year == dateTimeProvider.Now.Year
               && notionEvent.Start.Value.Month == dateTimeProvider.Now.Month
               && notionEvent.Start.Value.Day == dateTimeProvider.Now.Day;
    }

    public async Task<Message> SendMiniReminderMessageToChannel()
    {
        var events = await notionEventParserService.GetMiniReminders();
        var messageBody = $"""
                           <b>⚠️ Reminders</b> | <b>{events.Count} Reminders(s) Today</b>
                           --------------------------

                           """;
        foreach (var notionEvent in events)
        {
            var formattedDate = FormatEventDate(notionEvent);
            messageBody += $"""

                            For <b>🌟 <a href="{notionEvent.Url}">{notionEvent.Name}</a></b>, 
                            happening on {formattedDate},
                            be reminded that: {notionEvent.MiniReminderDesc}.

                            """;
        }

        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody,
            ParseMode.Html);
        return message;
    }
}