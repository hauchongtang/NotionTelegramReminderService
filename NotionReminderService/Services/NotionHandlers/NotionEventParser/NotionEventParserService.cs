using Notion.Client;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;

namespace NotionReminderService.Services.NotionHandlers.NotionEventParser;

public class NotionEventParserService(INotionService notionService, IDateTimeProvider dateTimeProvider, 
    ILogger<INotionEventParserService> logger)
    : INotionEventParserService
{
    public async Task<List<NotionEvent>> ParseEvent(bool isMorning)
    {
        DateTime from;
        DateTime to;
        if (isMorning)
        {
            var currentDt = dateTimeProvider.Now;
            from = new DateTime(currentDt.Year, currentDt.Month, currentDt.Day);
            to = currentDt.AddDays(3);
        }
        else
        {
            from = dateTimeProvider.Now;
            to = dateTimeProvider.Now.AddDays(3);
        }
        var pages = await GetPages(from, to);
        var events = new List<NotionEvent>();
        foreach (var page in pages.Results)
        {
            var notionEventName = GetNotionEventName(page);
            var location = GetNotionEventLocation(page);
            var persons = GetNotionEventPerson(page);
            var status = GetNotionEventStatus(page);
            var tags = GetNotionEventTag(page);
            var date = GetNotionEventDate(page);
            var notionEvent = GenerateNotionEvent(page, date, notionEventName, location, persons, status, tags);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        return events;
    }

    public async Task<List<NotionEvent>> GetOngoingEvents()
    {
        var from = dateTimeProvider.Now.AddMonths(-2);
        var to = dateTimeProvider.Now.AddMonths(2);
        var pages = await GetPages(from, to);
        var events = new List<NotionEvent>();
        foreach (var page in pages.Results)
        {
            var notionEventName = GetNotionEventName(page);
            var location = GetNotionEventLocation(page);
            var persons = GetNotionEventPerson(page);
            var status = GetNotionEventStatus(page);
            var tags = GetNotionEventTag(page);
            var date = GetNotionEventDate(page);
            var notionEvent = GenerateNotionEvent(page, date, notionEventName, location, persons, status, tags);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        var ongoingEvents = events.Where(e => e is { Start: not null, End: not null } 
                                              && dateTimeProvider.Now >= e.Start.Value && dateTimeProvider.Now <= e.End.Value);
        return ongoingEvents.ToList();
    }

    private static NotionEvent? GenerateNotionEvent(Page page, Date? date, string? notionEventName, string? location, string? persons, 
        string? status, string? tags)
    {
        if (date is null) return null;
        
        var notionEvent = new NotionEvent
        {
            Name = notionEventName,
            Where = location,
            Person = persons,
            Status = status,
            Tags = tags,
            Start = date.Start!.Value,
            End = date.End.HasValue ? date.End!.Value : null,
            Date = date.Start!.Value,
            Url = page.Url
        };

        return notionEvent;
    }

    private static Date? GetNotionEventDate(Page page)
    {
        if (!page.Properties.ContainsKey("Date")) return null;
        
        page.Properties.TryGetValue("Date", out var dateProperty);
        
        if (dateProperty is not { Type: PropertyValueType.Date }) return null;
        
        return ((DatePropertyValue) dateProperty).Date;
    }

    private static string? GetNotionEventLocation(Page page)
    {
        string? location = null;
        if (!page.Properties.ContainsKey("Where")) return location;
        page.Properties.TryGetValue("Where", out var where);
        var locRichText = ((RichTextPropertyValue) where!).RichText;
        location = locRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return location;
    }

    private static string? GetNotionEventName(Page page)
    {
        string? notionEventName = null;
        if (!page.Properties.ContainsKey("Name")) return notionEventName;
        page.Properties.TryGetValue("Name", out var title);
        var titleRichText = ((TitlePropertyValue) title!).Title;
        notionEventName = titleRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return notionEventName;
    }
    
    private static string? GetNotionEventPerson(Page page)
    {
        if (!page.Properties.ContainsKey("Person")) return null;
        
        page.Properties.TryGetValue("Person", out var personPropValue);
        var personList = ((PeoplePropertyValue) personPropValue!).People.Select(x => x.Name).ToList();
        return string.Join(", ", personList);
    }

    private static string? GetNotionEventStatus(Page page)
    {
        if (!page.Properties.ContainsKey("Status")) return null;

        page.Properties.TryGetValue("Status", out var statusPropValue);
        var status = ((StatusPropertyValue)statusPropValue!).Status;
        return status.Name;
    }

    private static string? GetNotionEventTag(Page page)
    {
        if (!page.Properties.ContainsKey("Tags")) return null;

        page.Properties.TryGetValue("Tags", out var multiSelectPropValue);
        var tags = ((MultiSelectPropertyValue)multiSelectPropValue!).MultiSelect.Select(x => x.Name);
        return string.Join(" | ", tags);
    }

    public async Task<PaginatedList<Page>> GetPages(DateTime from, DateTime to)
    {
        var betweenDates = GetDateBetweenFilter(from, to);
        
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = betweenDates,
            Sorts =
            [
                new Sort
                {
                    Direction = Direction.Ascending,
                    Property = "Date"
                }
            ]
        };
        var paginatedList = await notionService.GetPaginatedList(databaseQuery);
        return paginatedList;
    }

    public static CompoundFilter GetDateBetweenFilter(DateTime from, DateTime to)
    {
        var onOrBeforeDate = new DateFilter("Date", onOrBefore: to);
        var onOrAfterDate = new DateFilter("Date", onOrAfter: from);
        var betweenDates = new CompoundFilter
        {
            And =
            [
                onOrAfterDate,
                onOrBeforeDate
            ]
        };
        return betweenDates;
    }
}