using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Notion.Client;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Config;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.BotHandlers.TransportHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using User = Notion.Client.User;

namespace NotionReminderService.Services.BotHandlers.UpdateHandler;

public class UpdateService(
    ITelegramBotClient telegramBotClient,
    IWeatherMessageService weatherMessageService,
    INotionService notionService,
    IGoogleAiApi googleAiApi,
    ITransportService transportService,
    IDateTimeProvider dateTimeProvider,
    IOptions<NotionConfiguration> notionConfig,
    ILogger<IUpdateService> logger)
    : IUpdateService
{
    private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];

    public async Task HandleErrorAsync(Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message }                        => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll }                              => OnPoll(poll),
            { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        var messageText = msg.Text!.ToLower();
        if (messageText.Contains("/settings"))
        {
            await HandleSettings(msg, messageText);
        }
        else if (messageText.Contains("hi") && messageText.Contains("bot"))
        {
            await HandleAddNewEvent(msg, messageText);
        }
        else if (msg.Location is not null)
        {
            await HandleLocation(msg);
        }
    }

    private async Task HandleLocation(Message msg)
    {
            logger.LogInformation("Received location: {Location}", msg.Location);
            var location = msg.Location;
            var busArrivals = await transportService.GetNearestBusStops(location!.Latitude, location.Longitude, radius: 0.3);
            if (busArrivals is null)
            {
                await telegramBotClient.SendMessage(msg.Chat, "No bus stops found nearby.");
                return;
            }
            var messageBody = $"""
                               <b>{busArrivals.Count} Bus Stops Nearby:</b>
                               <b><i></i></b>
                               --------------------------

                               """;
            foreach (var busStop in busArrivals)
            {
                messageBody += $"""
                                <b>Bus Stop:</b> {busStop.BusStopDescription} - {busStop.BusStopCode}
                                <b>Bus Arrival Times:</b>
                                """;
                if (busStop.Services == null) continue;
                foreach (var busArrival in busStop.Services)
                {
                    messageBody += $"""
                                    <b>Bus Service:</b> {busArrival.ServiceNo} - {busArrival.Operator}
                                    """;
                    if (busArrival.NextBus is not null)
                    {
                        messageBody += $"""
                                        <b>Bus Arriving in </b> {dateTimeProvider.Now.Subtract(busArrival.NextBus.EstimatedArrival!.Value).Minutes}
                                        """;
                    }

                    if (busArrival.NextBus1 is not null)
                    {
                        messageBody += $"""
                                        ,{dateTimeProvider.Now.Subtract(busArrival.NextBus1.EstimatedArrival!.Value).Minutes}
                                        """;
                    }

                    if (busArrival.NextBus2 is not null)
                    {
                        messageBody += $"""
                                        ,{dateTimeProvider.Now.Subtract(busArrival.NextBus2.EstimatedArrival!.Value).Minutes}
                                        """;
                    }

                    messageBody += $"""
                                    minutes
                                    
                                    --------------------------
                                    """;
                }
            }
            await telegramBotClient.SendMessage(msg.Chat, messageBody, ParseMode.Html);
    }

    private async Task HandleSettings(Message msg, string messageText)
    {
        var messageTokens = messageText.Split("/settings");
        if (messageTokens.Length <= 1)
        {
            await telegramBotClient.SendMessage(msg.Chat, "Access denied. Please try again.");
            return;
        }

        // Validation Success -> Sends back inline message to admin
        var inlineOptionsForSettings = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData(text: "Set Telegram Webhook URL", callbackData: $"setTgWebhook"));
        await telegramBotClient.SendMessage(msg.Chat, "Hi Admin! Here is the settings menu below:", replyMarkup: inlineOptionsForSettings);
        return;
    }

    private async Task HandleAddNewEvent(Message msg, string messageText)
    {
        var newTitleFilter = new TitleFilter("Name", "GetAllTags");
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = newTitleFilter,
        };

        Page? tagsInfoPage;
        try
        {
            tagsInfoPage = await notionService.GetPageFromDatabase(databaseQuery);
        }
        catch (Exception e)
        {
            logger.LogError($"UpdateService.OnMessage NotionClient error -> {e}");
            // Inform user that the notion connector is down.
            await telegramBotClient.SendMessage(msg.Chat, 
                "The Notion Connector seems to be down right now! Please try again later.");
            return;
        }

        if (tagsInfoPage is null)
        {
            logger.LogError($"UpdateService.OnMessage -> No tag information can be found.");
            await telegramBotClient.SendMessage(msg.Chat, 
                "We are unable to generate your event. Please try again later.");
            return;
        }
            
        var persons = 
            PropertyValueParser<PeoplePropertyValue>.GetValueFromPage(tagsInfoPage, "Person");
        var p1 = PropertyValueParser<RichTextPropertyValue>
            .GetValueFromPage(tagsInfoPage, "PersonOneMap")?
            .RichText[0].PlainText;
        var p1Map = p1?.Split('-');
        var p2 = PropertyValueParser<RichTextPropertyValue>
            .GetValueFromPage(tagsInfoPage, "PersonTwoMap")?
            .RichText[0].PlainText;
        var p2Map = p2?.Split('-');
        var userId = msg.From!.Id; // Sender Id
        long.TryParse(p1Map?[0], out var p1Id);
        long.TryParse(p2Map?[0], out var p2Id);
        var sender = persons?.People.Find(x => 
            (p1Id == userId && x.Name == p1Map?[1])
            || (p2Id == userId && x.Name == p2Map?[1]));
            
        // var promptSb = new StringBuilder();
        // promptSb.Append($"This is the prompt: {messageText}");
        // promptSb.Append("Given this prompt, generate new event object. If there is no date detected, set it to today and end time to null.");
        // promptSb.Append($"For context, today's date is {dateTimeProvider.Now:yyyy MMMM dd}. My week begins on monday. " +
        // $"If today is sunday, next monday is the next day\n\n");
        // promptSb.Append("If there is no mini reminder description, set reminder_period and desc to null.");
        // promptSb.Append("Please parse the dates and times to the correct format.");
        // promptSb.Append("If there is no name, then based on the prompt, generate a name with less than 5 words.");
        // promptSb.Append($"Persons are {p1} and {p2}");
        // promptSb.Append($"Sender is {sender?.Name}. Please classify the persons based on the prompt.");
        // promptSb.Append("Detect based on the prompt, only classify to these two persons (one of whom is the sender). " +
        // "There can be 1 or 2 persons. Main identifiers are going with (who). If it is vague, just set the sender only." +
        // "Separate the persons with a `~`. Otherwise set to null.");
        // promptSb.Append("This is the response schema, please do it such that it is in escaped string format and parsable by dotnet: ");
        // promptSb.Append("Properties: {name: string, where: string, person: string?, tag: string?, start: datetime?, end: datetime?, reminder_period: string?, mini_reminder_desc: int?}");
        var prompt = $"""
                      	Instructions: Given this prompt, generate a new event json object.
                      	If you don't know the date, set the date to today and the time to null (For context, today is {dateTimeProvider.Now:yyyy MMMM dd}. My Week begins on Monday).
                      	Contd to the above: Datetime to be parsed to the ISO8601 format. However, if there is no time detected, then just send in YYYY-MM-DD format.
                      	If you don't detect any mini reminder description, then set reminder_period and desc to null.
                      	If there is no name, then based on the prompt, generate a name with less than 8 words.
                      	
                      	This is my prompt: {messageText}.
                      	Persons are {p1} and {p2}. Sender is {sender?.Name}. You will need to classify the persons based on the prompt.
                      	Detect based on the prompt, only classify to these two persons (one of whom is the sender).
                      	There can be only 1 or 2 persons. If it is vague, just set the sender only. Separate the persons with a `~`. Otherwise set to null.
                      	
                      	This is the response schema that you must strictly follow. Please do it such that it is in escaped string format and parsable by dotnet:
                      """;
        prompt += "Properties: {name: string, where: string, person: string?, tag: string?, start: datetime?, end: datetime?, reminder_period: string?, mini_reminder_desc: int?}";
        var messageResponse = await googleAiApi.GenerateContent(prompt);
            
        var eventObject = messageResponse.Candidates[0].Content.Parts[0].Text.Trim('\n').Trim('`');
        eventObject = eventObject.Replace("json", "");
        var notionNewEvent = JsonConvert.DeserializeObject<NotionNewEvent>(eventObject);
        if (notionNewEvent is null)
        {
            // Handle Parsing Error
            await telegramBotClient.SendMessage(msg.Chat,
                "Sorry, there is an error in creating the event. Please try again or do so via the Notion app.");
            return;
        }
            
        var personList = notionNewEvent.Person?.Split('~');
        var personsToAdd = new List<User?>();
        foreach (var person in personList!)
        {
            var pMap = person.Split('-');
            personsToAdd.Add(persons?.People.Find(x =>
                p1Id == userId && x.Name == pMap[1]));
        }
            
        var pageCreateParamsBuilder = PagesCreateParametersBuilder.Create(new DatabaseParentInput
        {
            DatabaseId = notionConfig.Value.DatabaseId
        });
        var pageCreateParams = pageCreateParamsBuilder
            .AddProperty("Name",
                new TitlePropertyValue
                {
                    Title = [new RichTextText { Text = new Text { Content = notionNewEvent.Name } }]
                })
            .AddProperty("Where",
                new RichTextPropertyValue
                    { RichText = [new RichTextText { Text = new Text { Content = notionNewEvent.Where } }] })
            .AddProperty("Date",
                new DatePropertyValue
                    { Date = new Date { Start = notionNewEvent.Start, End = notionNewEvent.End, TimeZone = "Asia/Singapore" } })
            .AddProperty("Person", new PeoplePropertyValue{ People = personsToAdd })
            .Build();
        var page = await notionService.CreateNewEvent(pageCreateParams);
        var notionEvent = NotionEventParser.GetNotionEvent(page);
        var formattedEventMsg = new NotionEventMessageBuilder().WithNotionEvent(notionEvent!, dateTimeProvider.Now).Build();
            
        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithUrl("Edit on Notion",
                page.Url));
        var tags = 
            PropertyValueParser<MultiSelectPropertyValue>.GetValueFromPage(tagsInfoPage, "Tags");
        tags?.MultiSelect.ForEach(tag =>
        {
            replyMarkup.AddNewRow()
                .AddButton(InlineKeyboardButton.WithCallbackData(text: $"Add tag: {tag.Name}", callbackData: $"{page.Id}~{tag.Name}"));
        });
        await telegramBotClient.SendMessage(msg.Chat,
            $"""
             Event created successfully! Here are the details:

             {formattedEventMsg}
             """, ParseMode.Html, replyMarkup: replyMarkup);
    }

    // Process Inline Keyboard callback data
    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        
        await telegramBotClient.AnswerCallbackQuery(callbackQuery.Id, $"Received request");
        
        switch (callbackQuery.Data)
        {
            case "triggerForecast":
            {
                await weatherMessageService.SendMessage(callbackQuery.Message!.Chat);
                break;
            }
            case "triggerCreateNewEventFlow":
            {
                await telegramBotClient.SendMessage(callbackQuery.Message!.Chat,
                    $"Please describe your new event with the Name, Location, Date, and Time.");
                break;
            }
            case "setTgWebhook":
            {
                // Redirect to custom webpage forms ?
                break;
            }
            default:
            {
                if (!callbackQuery.Data!.Contains('~'))
                {
                    await telegramBotClient.SendMessage(callbackQuery.Message!.Chat,
                        $"Feature is unavailable. It might be on maintenance or is disabled.");
                    break;
                }
                
                var data = callbackQuery.Data.Split('~');
                var pageId = data[0];
                var tagId = data[1];
                var updatedPage = await notionService.UpdatePageTag(pageId, tagId);
                var replyMarkup = new InlineKeyboardMarkup()
                    .AddNewRow()
                    .AddButton(InlineKeyboardButton.WithUrl("Edit on Notion",
                        updatedPage.Url));
                var updatedEvent = NotionEventParser.GetNotionEvent(updatedPage);
                var formattedEventMsg = new NotionEventMessageBuilder().WithNotionEvent(updatedEvent!, dateTimeProvider.Now).Build();
                await telegramBotClient.SendMessage(callbackQuery.Message?.Chat!,
                    $"""
                     Event updated successfully! Here are the details:

                     {formattedEventMsg}
                     """, ParseMode.Html, replyMarkup: replyMarkup);
                break;
            }
        }
    }

    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [ // displayed result
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await telegramBotClient.AnswerInlineQuery(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await telegramBotClient.SendMessage(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion

    private Task OnPoll(Poll poll)
    {
        logger.LogInformation("Received Poll info: {Question}", poll.Question);
        return Task.CompletedTask;
    }

    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var answer = pollAnswer.OptionIds.FirstOrDefault();
        var selectedOption = PollOptions[answer];
        if (pollAnswer.User != null)
            await telegramBotClient.SendMessage(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}