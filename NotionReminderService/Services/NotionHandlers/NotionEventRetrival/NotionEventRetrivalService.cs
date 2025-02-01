using Notion.Client;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;

namespace NotionReminderService.Services.NotionHandlers.NotionEventRetrival;

public class NotionEventRetrivalService(INotionService notionService, IDateTimeProvider dateTimeProvider, 
    ILogger<INotionEventRetrivalService> logger)
    : INotionEventRetrivalService
{
    public async Task<List<NotionEvent>> GetNotionEvents(bool isMorning)
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
            from = dateTimeProvider.Now.Date; // Consider events that have no time set -> Assumes whole day.
            to = dateTimeProvider.Now.AddDays(3);
            logger.LogInformation("NotionEventParserService.ParseEvent for (isMorning=false) --> From: {from} To: {to}",
                from, to);
        }
        
        var pages = await GetPages(from, to);
        var events = new List<NotionEvent>();
        foreach (var page in pages.Results)
        {
            try
            {
                var notionEvent = NotionEventParser.GetNotionEvent(page);
                if (notionEvent is null) continue;
                events.Add(notionEvent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }

        logger.LogInformation("NotionEventParserService.ParseEvent for (isMorning={isMorning}) --> {eventsCount} events to be sent",
            isMorning, events.Count);
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
            var notionEvent = NotionEventParser.GetNotionEvent(page);
            if (notionEvent is null) continue;
            events.Add(notionEvent);
        }

        var ongoingEvents = events.Where(e => e is { Start: not null, End: not null }).ToList();
        foreach (var e in ongoingEvents.ToList())
        {
            if (!IsEventStillOngoing(e))
            {
                ongoingEvents.Remove(e);
            }
        }
        
        logger.LogInformation("NotionEventParserService.GetOngoingEvents --> {eventCount} ongoing events to be sent",
            ongoingEvents.Count);
        return ongoingEvents.ToList();
    }

    public bool IsEventStillOngoing(NotionEvent e)
    {
        bool stillOngoing;
        if (e.End is { Hour: 0, Minute: 0, Second: 0 })
        {
            stillOngoing = e.Start != null && dateTimeProvider.Now >= e.Start.Value &&
                           dateTimeProvider.Now.Date <= e.End.Value;
        }
        else
        {
            stillOngoing = e is { End: not null, Start: not null } && dateTimeProvider.Now >= e.Start.Value &&
                           dateTimeProvider.Now <= e.End.Value;
        }

        return stillOngoing;
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
            var notionEvent = NotionEventParser.GetNotionEvent(page);
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