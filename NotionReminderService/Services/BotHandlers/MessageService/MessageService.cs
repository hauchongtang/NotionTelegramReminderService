using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Services.NotionHandlers;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NotionReminderService.Services.BotHandlers.MessageService;

public class MessageService(INotionEventParserService notionEventParserService, ITelegramBotClient telegramBotClient,
    IDateTimeProvider dateTimeProvider, IOptions<BotConfiguration> botConfig, IOptions<NotionConfiguration> notionConfig, 
    ILogger<IMessageService> logger)
    : IMessageService
{
    public async Task<Message> SendMessageToChannel(bool isMorning)
    {
        var events = await notionEventParserService.ParseEvent(isMorning);
        var ongoingEvents = await notionEventParserService.GetOngoingEvents();
        var greetings = isMorning ? "Morning" : "Evening";
        var messageBody = $"""
                           <a href="https://www.notion.so/{notionConfig.Value.DatabaseId}">View Plans on Notion</a> 
                           
                           <b><i>Good {greetings} All, there are {events.Count} events upcoming in the next 3 days.</i></b>
                           --------------------------
                           
                           """;
        foreach (var notionEvent in events)
        {
            var eventMessageFormat = $"""
                                      <b>Event: </b> <a href="{notionEvent.Url}">{notionEvent.Name}</a>
                                      <b>Where: </b> {notionEvent.Where}
                                      <b>Person(s): </b> {notionEvent.Person}
                                      <b>Status: </b> {notionEvent.Status}
                                      <b>Tags: </b> {notionEvent.Tags}
                                      <b>Date: </b> {notionEvent.Date!.Value:yyyy MMMM dd}
                                      <b>From: </b> {notionEvent.Start}
                                      <b>To: </b> {notionEvent.End}
                                      --------------------------
                                      
                                      """;
            messageBody += eventMessageFormat;
        }
        
        messageBody += $"""
                        
                        <b><i>Ongoing Events: </i></b>
                        --------------------------
                        
                        """;

        foreach (var notionEvent in ongoingEvents)
        {
            var eventMessageFormat = $"""
                                      <b>Event: </b> <a href="{notionEvent.Url}">{notionEvent.Name}</a>
                                      <b>Where: </b> {notionEvent.Where}
                                      <b>Person(s): </b> {notionEvent.Person}
                                      <b>Status: </b> {notionEvent.Status}
                                      <b>Tags: </b> {notionEvent.Tags}
                                      <b>Date: </b> {notionEvent.Date!.Value:yyyy MMMM dd}
                                      <b>From: </b> {notionEvent.Start}
                                      <b>To: </b> {notionEvent.End}
                                      --------------------------

                                      """;
            messageBody += eventMessageFormat;
        }

        messageBody += $"""
                        Updated as of: {dateTimeProvider.Now:F}
                        """;

        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody, ParseMode.Html);
        return message;
    }
}