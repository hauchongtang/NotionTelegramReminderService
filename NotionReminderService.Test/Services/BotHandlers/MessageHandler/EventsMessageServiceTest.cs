using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotionReminderService.Config;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.BotHandlers.MessageHandler;
using NotionReminderService.Services.NotionHandlers.NotionEventRetrival;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace NotionReminderService.Test.Services.BotHandlers.MessageHandler;

public class EventsMessageServiceTest {
    private Mock<INotionEventRetrivalService> _notionEventParserService;
    private Mock<ITelegramBotClient> _telegramBotClient;
    private Mock<IDateTimeProvider> _dateTimeProvider;
    private Mock<IOptions<BotConfiguration>> _botConfig;
    private Mock<IOptions<NotionConfiguration>> _notionConfig;
    private Mock<ILogger<EventsMessageService>> _logger;
    private EventsMessageService _eventsMessageService;

    [SetUp]
    public void Setup()
    {
        _notionEventParserService = new Mock<INotionEventRetrivalService>();
        _telegramBotClient = new Mock<ITelegramBotClient>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _botConfig = new Mock<IOptions<BotConfiguration>>();
        var config = new BotConfiguration
        {
            BotToken = "123",
            BotWebhookUrl = new Uri("https://www.url.org"),
            SecretToken = "123token",
            SecretTokenHeaderName = "X-Test-Header",
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
            x.GetNotionEvents(It.IsAny<bool>())).ReturnsAsync([
            new NotionEventBuilder()
                .WithName("Test Event").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something").WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 1").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something 1").WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 2").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something 2").WithReminderPeriodOptions(null)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 3").WithDate(DateTime.Now)
                .WithMiniReminderDesc(null).WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build()
        ]);
        _notionEventParserService.Setup(x => x.GetOngoingEvents()).ReturnsAsync([
            new NotionEventBuilder()
                .WithName("Test Event 3").WithDate(DateTime.Now)
                .WithStartDate(DateTime.Now).WithEndDate(DateTime.Now.AddDays(10))
                .WithMiniReminderDesc(null).WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build()
        ]);
        _telegramBotClient
            .Setup(x => x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        await _eventsMessageService.SendEventsMessageToChannel(isMorning: isMorning);
        
        _telegramBotClient.Verify(x =>
            x.SendRequest(It.Is<IRequest<Message>>(y => ((SendMessageRequest)y).Text.Contains(expectedToContain)),
                It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task SendMiniReminderMessageToChannel_HaveReminders_MessageSentToChannel()
    {
        _notionEventParserService.Setup(x => x.GetMiniReminders()).ReturnsAsync([
            new NotionEventBuilder()
                .WithName("Test Event").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something").WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 1").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something 1").WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 2").WithDate(DateTime.Now)
                .WithMiniReminderDesc("To do something 2").WithReminderPeriodOptions(null)
                .Build(),
            new NotionEventBuilder()
                .WithName("Test Event 3").WithDate(DateTime.Now)
                .WithMiniReminderDesc(null).WithReminderPeriodOptions(ReminderPeriodOptions.TwoDaysBefore)
                .Build()
        ]);
        _telegramBotClient
            .Setup(x => x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        var message = await _eventsMessageService.SendMiniReminderMessageToChannel();
        
        Assert.That(message, !Is.Null);
        _telegramBotClient.Verify(x =>
            x.SendRequest(
                It.Is<IRequest<Message>>(y =>
                    ((SendMessageRequest)y).Text.Contains("Test Event") 
                    && ((SendMessageRequest)y).Text.Contains("Test Event 1")
                    && !((SendMessageRequest)y).Text.Contains("Test Event 2")
                    && !((SendMessageRequest)y).Text.Contains("Test Event 3")),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}