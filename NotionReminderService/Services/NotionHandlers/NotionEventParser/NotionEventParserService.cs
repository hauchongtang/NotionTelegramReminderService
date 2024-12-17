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
            logger.LogInformation("NotionEventParserService.ParseEvent for (isMorning=true) --> From: {from} To: {to}",
                from, to);
        }
        else
        {
            from = dateTimeProvider.Now;
            to = dateTimeProvider.Now.AddDays(3);
            logger.LogInformation("NotionEventParserService.ParseEvent for (isMorning=false) --> From: {from} To: {to}",
                from, to);
        }
        
        var pages = await GetPages(from, to);
        var events = new List<NotionEvent>();
        foreach (var page in pages.Results)
        {
            var notionEvent = GetNotionEvent(page);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        logger.LogInformation("NotionEventParserService.ParseEvent for (isMorning={isMorning}) --> {eventsCount} events to be sent",
            events.Count, isMorning);
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
            var notionEvent = GetNotionEvent(page);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        var ongoingEvents = events.Where(e => e is { Start: not null, End: not null } 
                                              && dateTimeProvider.Now >= e.Start.Value && dateTimeProvider.Now <= e.End.Value);
        logger.LogInformation("NotionEventParserService.GetOngoingEvents --> {eventCount} ongoing events to be sent",
            events.Count);
        return ongoingEvents.ToList();
    }

    private static NotionEvent? GetNotionEvent(Page page)
    {
        var notionEventName = GetNotionEventName(page);
        var location = GetNotionEventLocation(page);
        var persons = GetNotionEventPerson(page);
        var status = GetNotionEventStatus(page);
        var tags = GetNotionEventTag(page);
        var date = GetNotionEventDate(page);
        var miniReminderDesc = GetNotionMiniReminderDescription(page);
        var miniReminderTrigger = GetNotionMiniReminderTrigger(page);
        var notionEvent = GenerateNotionEvent(page, date, notionEventName, location, persons, status, tags,
            miniReminderDesc, miniReminderTrigger);
        return notionEvent;
    }

    private static NotionEvent? GenerateNotionEvent(Page page, Date? date, string? notionEventName, string? location,
        string? persons, string? status, string? tags, string? miniReminderDesc, ReminderPeriodOptions? options)
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
            Url = page.Url,
            MiniReminderDesc = miniReminderDesc,
            ReminderPeriod = options
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

    private static ReminderPeriodOptions? GetNotionMiniReminderTrigger(Page page)
    {
        if (!page.Properties.ContainsKey("Trigger Mini Reminder")) return null;

        page.Properties.TryGetValue("Trigger Mini Reminder", out var selectPropValue);
        var triggerProperty = ((SelectPropertyValue)selectPropValue!).Select?.Name;
        return triggerProperty switch
        {
            "On the day itself" => ReminderPeriodOptions.OnTheDayItself,
            "One day before" => ReminderPeriodOptions.OneDayBefore,
            "Two days before" => ReminderPeriodOptions.TwoDaysBefore,
            "One week before" => ReminderPeriodOptions.OneWeekBefore,
            _ => null
        };
    }

    private static string? GetNotionMiniReminderDescription(Page page)
    {
        if (!page.Properties.ContainsKey("Mini Reminder Description")) return null;

        page.Properties.TryGetValue("Mini Reminder Description", out var richTextPropValue);
        var description =
            ((RichTextPropertyValue)richTextPropValue!).RichText.Aggregate("", (s, rt) => s + rt.PlainText);
        return description;
    }

    private async Task<PaginatedList<Page>> GetPages(DateTime from, DateTime to)
    {
        var betweenDates = GetDateBetweenFilter("Date", from, to);
        
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

    public static CompoundFilter GetDateBetweenFilter(string propertyName, DateTime from, DateTime to)
    {
        var onOrBeforeDate = new DateFilter(propertyName, onOrBefore: to);
        var onOrAfterDate = new DateFilter(propertyName, onOrAfter: from);
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

    public async Task<List<NotionEvent>> GetMiniReminders()
    {
        var eventsWithMiniReminders = await GetEventsWithMiniReminders();
        var events = new List<NotionEvent>();
        foreach (var page in eventsWithMiniReminders.Results)
        {
            var notionEvent = GetNotionEvent(page);
            if (notionEvent is null) continue;
            if (notionEvent.MiniReminderDesc is null && notionEvent.ReminderPeriod is null) continue;
            if (!IsMiniReminderToTriggerToday(notionEvent)) continue;
            
            events.Add(notionEvent);
        }

        logger.LogInformation("NotionEventParserService.GetMiniReminders --> {eventCount} mini reminders to be sent.",
            events.Count);
        return events;
    }

    private bool IsMiniReminderToTriggerToday(NotionEvent notionEvent)
    {
        if (notionEvent.MiniReminderDesc is null || notionEvent.ReminderPeriod is null) return false;
        if (notionEvent.Start is null) return false;

        return notionEvent.ReminderPeriod switch
        {
            ReminderPeriodOptions.OnTheDayItself => notionEvent.Start.Value.Date == dateTimeProvider.Now.Date,
            ReminderPeriodOptions.OneDayBefore => notionEvent.Start.Value.Date.AddDays(-1) == dateTimeProvider.Now.Date,
            ReminderPeriodOptions.TwoDaysBefore =>
                notionEvent.Start.Value.Date.AddDays(-2) == dateTimeProvider.Now.Date,
            ReminderPeriodOptions.OneWeekBefore =>
                notionEvent.Start.Value.Date.AddDays(-7) == dateTimeProvider.Now.Date,
            _ => false
        };
    }

    private async Task<PaginatedList<Page>> GetEventsWithMiniReminders()
    {
        var filter = GetDateBetweenFilter("Date", dateTimeProvider.Now.Date,
            dateTimeProvider.Now.Date.AddDays(8));
        filter.And.Add(new RichTextFilter("Mini Reminder Description", isNotEmpty: true));
        filter.And.Add(new SelectFilter("Trigger Mini Reminder", isNotEmpty: true));
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = filter,
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
}