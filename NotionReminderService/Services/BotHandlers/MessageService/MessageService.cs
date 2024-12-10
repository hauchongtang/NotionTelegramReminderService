using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Services.NotionHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NotionReminderService.Services.BotHandlers.MessageService;

public class MessageService(INotionEventParserService notionEventParserService, ITelegramBotClient telegramBotClient, 
    IOptions<BotConfiguration> botConfig, ILogger<IMessageService> logger)
    : IMessageService
{
    public async Task<Message> SendMessageToChannel(bool isMorning)
    {
        var events = await notionEventParserService.ParseEvent(isMorning);
        var ongoingEvents = await notionEventParserService.GetOngoingEvents();
        var greetings = isMorning ? "Morning" : "Evening";
        var messageBody = $"""
                           Updated as of: {DateTime.Now:F}
                           --------------------------
                           <b><i>Good {greetings} All, there are {events.Count} events upcoming in the next 3 days.</i></b>
                           --------------------------
                           
                           """;
        foreach (var notionEvent in events)
        {
            var eventMessageFormat = $"""
                                      <b>Event: </b> {notionEvent.Name}
                                      <b>Where: </b> {notionEvent.Where}
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
                                      <b>Event: </b> {notionEvent.Name}
                                      <b>Where: </b> {notionEvent.Where}
                                      <b>Date: </b> {notionEvent.Date!.Value:yyyy MMMM dd}
                                      <b>From: </b> {notionEvent.Start}
                                      <b>To: </b> {notionEvent.End}
                                      --------------------------

                                      """;
            messageBody += eventMessageFormat;
        }

        var message = await telegramBotClient.SendMessage(new ChatId(botConfig.Value.ChatId), messageBody, ParseMode.Html);
        return message;
    }
}