using Microsoft.Extensions.Logging;
using Moq;
using Notion.Client;
using NotionReminderService.Services.NotionHandlers;
using NotionReminderService.Services.NotionHandlers.NotionEventParser;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Test.TestUtils.Page;
using NotionReminderService.Utils;

namespace NotionReminderService.Test.Services.NotionHandlers;

public class NotionEventParserServiceTest
{
    private Mock<INotionService> _notionService;
    private Mock<ILogger<INotionEventParserService>> _logger;
    private NotionEventParserService _notionEventParserService;
    private Mock<IDateTimeProvider> _dateTimeProvider;

    [SetUp]
    public void SetUp()
    {
        _notionService = new Mock<INotionService>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _logger = new Mock<ILogger<INotionEventParserService>>();
        _notionEventParserService =
            new NotionEventParserService(_notionService.Object, _dateTimeProvider.Object, _logger.Object);
    }

    [Test]
    public async Task ParseEvent_ValidEvents_Success()
    {
        const bool isMorning = true;
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build() },
                        { "Location", new RichTextPropertyBuilder().WithText("Singapore").Build() }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build() },
                        { "Location", new RichTextPropertyBuilder().WithText("Hong Kong").Build() }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.ParseEvent(isMorning);
        
        Assert.That(events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ParseEvent_EventNoDate_NotAddedToList()
    {
        const bool isMorning = true;
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                        { { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() } }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build() }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.ParseEvent(isMorning);
        
        Assert.That(!events.Exists(x => x.Date is null));
    }

    [Test]
    public async Task ParseEvent_WrongPropertyValueForDate_NotAddedToList()
    {
        const bool isMorning = true;
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                        { { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() } }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new TitlePropertyBuilder().WithTitle("Event 2").Build() }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.ParseEvent(isMorning);
        
        Assert.That(!events.Exists(x => x.Name == "Event 2"));
    }

    [Test]
    public async Task GetOngoingEvents_CurrentDayOnEventStartAndBeforeEventEnd_ShowsEvent()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .Build()
                        }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                            .Build()
                        }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.GetOngoingEvents();

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events.Exists(x => x.Name == "Event 2"));
    }

    [Test]
    public async Task GetOngoingEvents_CurrentDayOnEventEnd_ShowsEvent()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .Build()
                        }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(11).WithDay(1).WithHour(12).Build())
                            .WithEndDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .Build()
                        }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.GetOngoingEvents();

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events.Exists(x => x.Name == "Event 2"));
    }

    [Test]
    public async Task GetOngoingEvents_CurrentDayBetweenEventStartEnd_ShowsEvent()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .Build()
                        }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(11).WithDay(1).WithHour(12).Build())
                            .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                            .Build()
                        }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.GetOngoingEvents();

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events.Exists(x => x.Name == "Event 2"));
    }

    [Test]
    public async Task GetOngoingEvents_CurrentDayBeforeEventStart_EventNotShown()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(30).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).WithMinute(30).Build())
                            .Build()
                        }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(31).Build())
                            .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                            .Build()
                        }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.GetOngoingEvents();

        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task GetOngoingEvents_CurrentDayAfterEventEnds_EventNotShown()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).WithHour(12).WithMinute(31).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                            .Build()
                        }
                    }
                },
                new Page
                {
                    Properties = new Dictionary<string, PropertyValue>
                    {
                        { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                        { "Date", new DatePropertyBuilder()
                            .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                            .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).WithHour(12).WithMinute(30).Build())
                            .Build()
                        }
                    }
                }
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventParserService.GetOngoingEvents();

        Assert.That(events, Is.Empty);
    }
}