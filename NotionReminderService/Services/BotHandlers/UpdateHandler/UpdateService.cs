using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Notion.Client;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Config;
using NotionReminderService.Models.NotionEvent;
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

namespace NotionReminderService.Services.BotHandlers.UpdateHandler;

public class UpdateService(
    ITelegramBotClient telegramBotClient,
    IWeatherMessageService weatherMessageService,
    INotionService notionService,
    IGoogleAiApi googleAiApi,
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
            // { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll }                              => OnPoll(poll),
            { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            // ChannelPost:
            // EditedChannelPost:
            // ShippingQuery:
            // PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        var messageText = msg.Text;

        if (messageText!.Contains("event") && (messageText.Contains("at") || messageText.Contains("from")))
        {
            var promptSb = new StringBuilder();
            promptSb.Append($"This is the prompt: {messageText}");
            promptSb.Append("Given this prompt, generate new event object. If there is no date detected, set it to today and end time to null.");
            promptSb.Append($"For context, today's date is {dateTimeProvider.Now:yyyy MMMM dd}. My week begins on monday. " +
                            $"If today is sunday, next monday is the next day\n\n");
            promptSb.Append("If there is no mini reminder description, set reminder_period and desc to null.");
            promptSb.Append("Please parse the dates and times to the correct format.");
            promptSb.Append("If there is no name, then based on the prompt, generate a name with less than 5 words.");
            promptSb.Append("This is the response schema, please do it such that it is in escaped string format and parsable by dotnet: ");
            promptSb.Append("Properties: {name: string, where: string, start: datetime?, end: datetime?, reminder_period: string?, mini_reminder_desc: int?}");
            var messageResponse = await googleAiApi.GenerateContent(promptSb.ToString());
            var eventObject = messageResponse.Candidates[0].Content.Parts[0].Text.Trim('\n').Trim('`');
            eventObject = eventObject.Replace("json", "");
            
            var notionNewEvent = JsonConvert.DeserializeObject<NotionNewEvent>(eventObject);
            if (notionNewEvent is null)
            {
                // Handle Parsing Error
                await telegramBotClient.SendMessage(msg.Chat,
                    "Sorry, there is an error in creating the event. Please do so via the Notion app.");
                return;
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
                .Build();
            
            var page = await notionService.CreateNewEvent(pageCreateParams);
            var notionEvent = NotionEventParser.GetNotionEvent(page);
            var formattedEventMsg = new NotionEventMessageBuilder().WithNotionEvent(notionEvent!, dateTimeProvider.Now).Build();

            var replyMarkup = new InlineKeyboardMarkup()
                .AddNewRow()
                .AddButton(InlineKeyboardButton.WithUrl("Edit on Notion",
                    page.Url));
            await telegramBotClient.SendMessage(msg.Chat,
                $"""
                Event created successfully! Here are the details:
                
                {formattedEventMsg}
                """, ParseMode.Html, replyMarkup: replyMarkup);
        }
    }

    async Task<Message> Usage(Message msg)
    {
        const string usage = """
                <b><u>Bot menu</u></b>:
                /photo          - send a photo
                /inline_buttons - send inline buttons
                /keyboard       - send keyboard buttons
                /remove         - remove keyboard buttons
                /request        - request location or contact
                /inline_mode    - send inline-mode results list
                /poll           - send a poll
                /poll_anonymous - send an anonymous poll
                /throw          - what happens if handler fails
            """;
        return await telegramBotClient.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }

    async Task<Message> SendPhoto(Message msg)
    {
        await telegramBotClient.SendChatAction(msg.Chat, ChatAction.UploadPhoto);
        await Task.Delay(2000); // simulate a long task
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await telegramBotClient.SendPhoto(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow()
                .AddButton("WithCallbackData", "CallbackData")
                .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        return await telegramBotClient.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow().AddButton("2.1").AddButton("2.2");
        return await telegramBotClient.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
    }

    async Task<Message> RemoveKeyboard(Message msg)
    {
        return await telegramBotClient.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    async Task<Message> RequestContactAndLocation(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddButton(KeyboardButton.WithRequestLocation("Location"))
            .AddButton(KeyboardButton.WithRequestContact("Contact"));
        return await telegramBotClient.SendMessage(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
    }

    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await telegramBotClient.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    async Task<Message> SendPoll(Message msg)
    {
        return await telegramBotClient.SendPoll(msg.Chat, "Question", PollOptions, isAnonymous: false);
    }

    async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await telegramBotClient.SendPoll(chatId: msg.Chat, "Question", PollOptions);
    }

    static Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("FailingHandler");
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
            default:
            {
                await telegramBotClient.SendMessage(callbackQuery.Message!.Chat,
                    $"Feature is unavailable. It might be on maintenance or is disabled.");
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