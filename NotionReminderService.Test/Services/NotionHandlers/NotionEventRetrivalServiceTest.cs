using Microsoft.Extensions.Logging;
using Moq;
using Notion.Client;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.NotionHandlers.NotionEventRetrival;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Test.TestUtils.Page;
using NotionReminderService.Utils;

namespace NotionReminderService.Test.Services.NotionHandlers;

public class NotionEventRetrivalServiceTest
{
    private Mock<INotionService> _notionService;
    private Mock<ILogger<INotionEventRetrivalService>> _logger;
    private NotionEventRetrivalService _notionEventRetrivalService;
    private Mock<IDateTimeProvider> _dateTimeProvider;

    [SetUp]
    public void SetUp()
    {
        _notionService = new Mock<INotionService>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _logger = new Mock<ILogger<INotionEventRetrivalService>>();
        _notionEventRetrivalService =
            new NotionEventRetrivalService(_notionService.Object, _dateTimeProvider.Object, _logger.Object);
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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetNotionEvents(isMorning);
        
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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetNotionEvents(isMorning);
        
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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetNotionEvents(isMorning);
        
        Assert.That(!events.Exists(x => x.Name == "Event 1"));
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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetOngoingEvents();

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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(11).WithDay(1).WithHour(12).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetOngoingEvents();

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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(11).WithDay(1).WithHour(12).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetOngoingEvents();

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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).WithMinute(30).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(31).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetOngoingEvents();

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
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(12).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).WithHour(12).WithMinute(30).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Hong Kong").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User1234"}).Build())
                    .Build()
            ]
        };
        _notionService.Setup(x =>
            x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var events = await _notionEventRetrivalService.GetOngoingEvents();

        Assert.That(events, Is.Empty);
    }

    [Test]
    public void IsEventStillOngoing_StartDateIsTodayWithoutTimeSpecified_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(16).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_StartDateIsTodayWithTimeSpecified_TimeBeforeNow_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(9).WithMinute(59).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_StartDateIsTodayWithTimeSpecified_TimeAfterNow_NotOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(9).WithMinute(59).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.False);
    }
    
    [Test]
    public void IsEventStillOngoing_StartDateIsTodayWithTimeSpecified_TimeIsNow_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_EndDateIsTodayWithoutTimeSpecified_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_EndDateIsTodayWithTimeSpecified_TimeBeforeNow_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(9).WithMinute(59).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_EndDateIsTodayWithTimeSpecified_TimeIsNow_StillOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.True);
    }

    [Test]
    public void IsEventStillOngoing_EndDateIsTodayWithTimeSpecified_TimeAfterNow_NotOngoing()
    {
        _dateTimeProvider.Setup(x => x.Now)
            .Returns(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(10).Build());
        var notionEvent = new NotionEventBuilder()
            .WithName("Ongoing event")
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build())
            .WithEndDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(9).WithMinute(59).Build())
            .Build();

        var eventStillOngoing = _notionEventRetrivalService.IsEventStillOngoing(notionEvent);
        
        Assert.That(eventStillOngoing, Is.False);
    }

    [Test]
    public async Task GetMiniReminders_MixOfEventsWithAndWithoutMiniReminders_ReturnsRemindersToBeTriggeredToday()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(30).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstDec2024);
        
        var paginatedList = new PaginatedList<Page>
        {
            Results =
            [
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(2).WithHour(12)
                            .WithMinute(30).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .WithProperty("Mini Reminder Description", new RichTextPropertyBuilder().WithText("To do something").Build())
                    .WithProperty("Trigger Mini Reminder",
                        new SelectPropertyBuilder().WithSelectOption(new SelectOption { Name = "One day before" })
                            .Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 2").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(31).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .WithProperty("Mini Reminder Description", new RichTextPropertyBuilder().WithText("To do something").Build())
                    .WithProperty("Trigger Mini Reminder",
                        new SelectPropertyBuilder().WithSelectOption(new SelectOption { Name = "One day before" })
                            .Build())
                    .Build(),
                new NotionPageBuilder()
                    .WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 3").Build())
                    .WithProperty("Date", new DatePropertyBuilder()
                        .WithStartDt(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).WithHour(13).WithMinute(31).Build())
                        .WithEndDt(new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build())
                        .Build())
                    .WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
                    .WithProperty("Person", new PeoplePropertyBuilder().WithUser(new User{ Name = "User123"}).Build())
                    .WithProperty("Mini Reminder Description", new RichTextPropertyBuilder().WithText("To do something").Build())
                    .WithProperty("Trigger Mini Reminder",
                        new SelectPropertyBuilder().WithSelectOption(new SelectOption { Name = "One day before" })
                            .Build())
                    .Build()
            ]
        };
        _notionService.Setup(x => x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(paginatedList);

        var miniReminders = await _notionEventRetrivalService.GetMiniReminders();
        
        Assert.That(miniReminders, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(miniReminders[0].Name, Is.EqualTo("Event 1"));
            Assert.That(miniReminders[0].MiniReminderDesc, Is.EqualTo("To do something"));
            Assert.That(miniReminders[0].ReminderPeriod, Is.EqualTo(ReminderPeriodOptions.OneDayBefore));
        });
    }
}