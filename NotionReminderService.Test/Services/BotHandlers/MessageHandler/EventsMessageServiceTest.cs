using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotionReminderService.Config;
using NotionReminderService.Services.BotHandlers.MessageHandler;
using NotionReminderService.Services.NotionHandlers;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace NotionReminderService.Test.Services.BotHandlers.MessageHandler;

public class EventsMessageServiceTest {
    private Mock<INotionEventParserService> _notionEventParserService;
    private Mock<ITelegramBotClient> _telegramBotClient;
    private Mock<IDateTimeProvider> _dateTimeProvider;
    private Mock<IOptions<BotConfiguration>> _botConfig;
    private Mock<IOptions<NotionConfiguration>> _notionConfig;
    private Mock<ILogger<EventsMessageService>> _logger;
    private EventsMessageService _eventsMessageService;

    [SetUp]
    public void Setup()
    {
        _notionEventParserService = new Mock<INotionEventParserService>();
        _telegramBotClient = new Mock<ITelegramBotClient>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _botConfig = new Mock<IOptions<BotConfiguration>>();
        var config = new BotConfiguration
        {
            BotToken = "123",
            BotWebhookUrl = new Uri("https://www.url.org"),
            SecretToken = "123token",
            ChatId = "456"
        };
        _botConfig.Setup(x => x.Value).Returns(config);
        _notionConfig = new Mock<IOptions<NotionConfiguration>>();
        var notionConfig = new NotionConfiguration
        {
            NotionAuthToken = "123",
            DatabaseId = "123"
        };
        _notionConfig.Setup(x => x.Value).Returns(notionConfig);
        _logger = new Mock<ILogger<EventsMessageService>>();
        _eventsMessageService = new EventsMessageService(_notionEventParserService.Object, _telegramBotClient.Object,
            _dateTimeProvider.Object, _botConfig.Object, _notionConfig.Object, _logger.Object);
    }

    [Test]
    [TestCase(true, "Morning")]
    [TestCase(false, "Evening")]
    public async Task SendEventsMessageToChannel_MessageTime_MessageTimeGreetingsInMessageBody(bool isMorning, string expectedToContain)
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build());
        _notionEventParserService.Setup(x =>
            x.ParseEvent(It.IsAny<bool>())).ReturnsAsync([]);
        _notionEventParserService.Setup(x => x.GetOngoingEvents()).ReturnsAsync([]);
        _telegramBotClient
            .Setup(x => x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        await _eventsMessageService.SendEventsMessageToChannel(isMorning: isMorning);
        
        _telegramBotClient.Verify(x =>
            x.SendRequest(It.Is<IRequest<Message>>(y => ((SendMessageRequest)y).Text.Contains(expectedToContain)),
                It.IsAny<CancellationToken>()));
    }
    
    // new NotionEventBuilder()
    //     .WithName("Event 1")
    //     .WithLocation("KL")
    //     .WithPerson("Person1, Person2")
    //     .WithStatus("Status 1")
    //     .WithTags("Tag 1")
    //     .WithDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
    //     .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
    //     .WithUrl("www.123123123123.org")
    //     .Build(),
    // new NotionEventBuilder()
    //     .WithName("Event 2")
    //     .WithLocation("SG")
    //     .WithPerson("Person2")
    //     .WithStatus("Status 9")
    //     .WithTags("Tag 0")
    //     .WithDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
    //     .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
    //     .WithUrl("www.123123123123.org")
    //     .Build()
}