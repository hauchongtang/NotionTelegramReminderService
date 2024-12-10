using Microsoft.Extensions.Options;
using Notion.Client;
using NotionReminderService.Config;
using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Services.NotionHandlers;

public class NotionEventParserService(INotionClient notionClient, IOptions<NotionConfiguration> options, 
    ILogger<INotionEventParserService> logger)
    : INotionEventParserService
{
    public async Task<List<NotionEvent>> ParseEvent(bool isMorning)
    {
        DateTime from;
        DateTime to;
        if (isMorning)
        {
            var currentDt = DateTime.Now;
            from = new DateTime(currentDt.Year, currentDt.Month, currentDt.Day);
            to = currentDt.AddDays(3);
        }
        else
        {
            from = DateTime.Now;
            to = DateTime.Now.AddDays(3);
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
            var notionEvent = GenerateNotionEvent(page, notionEventName, location, persons, status, tags);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        return events;
    }

    public async Task<List<NotionEvent>> GetOngoingEvents()
    {
        var from = DateTime.Now.AddMonths(-2);
        var to = DateTime.Now.AddMonths(2);
        var pages = await GetPages(from, to);
        var events = new List<NotionEvent>();
        foreach (var page in pages.Results)
        {
            var notionEventName = GetNotionEventName(page);
            var location = GetNotionEventLocation(page);
            var persons = GetNotionEventPerson(page);
            var status = GetNotionEventStatus(page);
            var tags = GetNotionEventTag(page);
            var notionEvent = GenerateNotionEvent(page, notionEventName, location, persons, status, tags);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        var ongoingEvents = events.Where(e => e is { Start: not null, End: not null } 
                                              && DateTime.Now >= e.Start.Value && DateTime.Now <= e.End.Value);
        return ongoingEvents.ToList();
    }

    private static NotionEvent? GenerateNotionEvent(Page page, string? notionEventName, string? location, string? persons, 
        string? status, string? tags)
    {
        NotionEvent? notionEvent = null;
        if (!page.Properties.ContainsKey("Date")) return null;
        
        page.Properties.TryGetValue("Date", out var dateProperty);
        
        if (dateProperty is not { Type: PropertyValueType.Date }) return null;
        
        var date = ((DatePropertyValue) dateProperty).Date;
        if (date.Start.HasValue && date.End.HasValue)
        {
            notionEvent = new NotionEvent
            {
                Name = notionEventName,
                Where = location,
                Person = persons,
                Status = status,
                Tags = tags,
                Start = date.Start.Value,
                End = date.End.Value,
                Date = date.Start.Value,
                Url = page.Url
            };
        }

        else
            notionEvent = date switch
            {
                { Start: not null, End: null } => new NotionEvent
                {
                    Name = notionEventName, Where = location, Start = date.Start.Value, Date = date.Start.Value, 
                    Person = persons, Status = status, Tags = tags, Url = page.Url
                },
                { Start: null, End: null } => new NotionEvent
                {
                    Name = notionEventName, Where = location, Date = date.Start.Value, Person = persons,
                    Status = status, Tags = tags, Url = page.Url
                },
                _ => notionEvent
            };

        return notionEvent;
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
        return string.Join(" ,", personList);
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
        var paginatedList = await notionClient.Databases.QueryAsync(options.Value.DatabaseId, databaseQuery);
        return paginatedList;
    }

    public CompoundFilter GetDateBetweenFilter(DateTime from, DateTime to)
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