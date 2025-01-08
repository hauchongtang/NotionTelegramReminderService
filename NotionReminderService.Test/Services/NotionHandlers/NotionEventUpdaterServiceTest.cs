using Microsoft.Extensions.Logging;
using Moq;
using Notion.Client;

public class NotionEventParserServiceTest 
{
    [Setup]
    public void SetUp() {
        _notionService = new Mock<INotionService>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _logger = new Mock<ILogger<INotionEventUpdaterService>>();
        _notionEventParserService = new _notionEventParserService(_notionService.Object, 
            _dateTimeProvider.Object, _logger.Object);
    }

    [Test]
    public async Task UpdateEventsToCompleted_NotionConnectorIsWorking_ReturnsPage() 
    {
        var firstJan2025 = new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(firstJan2025);
        
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
        _notionService.Setup(x => x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).Returns(paginatedList);

        var events = await _notionEventParserService.UpdateEventsToCompleted(paginatedList);

        Assert.That(events, Has.Count.Equals(1));
    }

    [Test]
    public async Task UpdateEventsToInProgress_IsMorningJob_DateTimeToPlus12()
    {
        var firstJan2025 = new DateTimeBuilder().WithYear(2025)
            .WithMonth(1).WithDay(1).Build();

        var paginatedList = new PaginatedListBuilder()
        .AddNewPage(
            new Page
            {
                Properties = new Dictionary<string, PropertyValue>
                { { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() } }
            }
        )
        .AddNewPage(
            new Page
            {
                Properties = new Dictionary<string, PropertyValue>
                {
                    { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                    { "Date", new TitlePropertyBuilder().WithTitle("Event 2").Build() }
                }
            }
        ).Build();
        _notionService.Setup(x => x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).Returns(paginatedList);

        var events = await _notionEventParserService.UpdateEventsToInProgress(paginatedList);

        Assert.That(events, Has.Count.Equals(1));
    }

    [Test]
    public async Task UpdateEventsToInProgress_NotMorningJob_SecondHalfOfDayRange()
    {
        var firstJan2025 = new DateTimeBuilder().WithYear(2025).WithMonth(1).WithDay(1);
        var paginatedList = new PaginatedListBuilder()
        .AddNewPage(
            new Page
            {
                Properties = new Dictionary<string, PropertyValue>
                { { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() } }
            }
        )
        .AddNewPage(
            new Page
            {
                Properties = new Dictionary<string, PropertyValue>
                {
                    { "Name", new TitlePropertyBuilder().WithTitle("Event 2").Build() },
                    { "Date", new TitlePropertyBuilder().WithTitle("Event 2").Build() }
                }
            }
        ).Build();
        _notionService.Setup(x => x.GetPaginatedList(It.IsAny<DatabasesQueryParameters>())).Returns(paginatedList);

        var events = await _notionEventParserService.UpdateEventsToInProgress(paginatedList);

        Assert.That(events, Has.Count.Equals(1));
    }
}