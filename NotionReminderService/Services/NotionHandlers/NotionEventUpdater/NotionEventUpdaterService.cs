using Notion.Client;
using NotionReminderService.Services.NotionHandlers.NotionEventRetrival;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;

namespace NotionReminderService.Services.NotionHandlers.NotionEventUpdater;

public class NotionEventUpdaterService(
    INotionService notionService,
    IDateTimeProvider dateTime,
    ILogger<INotionEventUpdaterService> logger) : INotionEventUpdaterService
{
    public async Task<List<Page>> UpdateEventsToCompleted()
    {
        var eventsThatEndsToday = await GetEventsEndingToday();
        return await notionService.UpdateEventsToCompleted(eventsThatEndsToday);
    }

    private async Task<PaginatedList<Page>> GetEventsEndingToday()
    {
        var dateTimeAtStartOfDay = dateTime.Now.Date.AddMonths(-2);
        var dateTimeAtEndOfDay = dateTime.Now.Date.AddHours(23).AddMinutes(59);
        
        logger.LogInformation("NotionEventUpdaterService.GetEventsEndingToday --> from: {from} to: {to}",
            dateTimeAtStartOfDay, dateTimeAtEndOfDay);
        
        var dateFilter = NotionEventRetrivalService.GetDateBetweenFilter(propertyName: "Date", from: dateTimeAtStartOfDay,
            to: dateTimeAtEndOfDay);
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = dateFilter,
            Sorts =
            [
                new Sort
                {
                    Direction = Direction.Ascending,
                    Property = "Date"
                }
            ]
        };
        var endsTodayEventList = await notionService.GetPaginatedList(databaseQuery);
        endsTodayEventList.Results = endsTodayEventList.Results.Where(x =>
            ((StatusPropertyValue)x.Properties["Status"]).Status.Name != "Done"
            &&
            (
                ((DatePropertyValue)x.Properties["Date"]).Date.End <= dateTimeAtEndOfDay
                ||
                (
                    ((DatePropertyValue)x.Properties["Date"]).Date.End is null 
                    &&
                    ((DatePropertyValue)x.Properties["Date"]).Date.Start <= dateTimeAtEndOfDay)
                )
            ).ToList();

        logger.LogInformation("NotionEventUpdaterService.GetEventsEndingToday --> {eventCount} event(s) end today.",
            endsTodayEventList.Results.Count);
        
        return endsTodayEventList;
    }

    public async Task<List<Page>> UpdateEventsToInProgress(bool isMorningJob)
    {
        PaginatedList<Page> eventsToUpdate;
        if (isMorningJob)
        {
            var from = dateTime.Now.Date;
            var to = dateTime.Now.Date.AddHours(12);
            
            logger.LogInformation(
                "NotionEventUpdaterService.UpdateEventsToInProgress (isMorning=true) --> Retrieving events from: {from} to: {to}", from,
                to);
            
            eventsToUpdate = await GetEvents(from, to);
        }
        else
        {
            var from = dateTime.Now.Date.AddHours(12).AddMinutes(1);
            var to = dateTime.Now.Date.AddHours(23).AddMinutes(59);
            
            logger.LogInformation(
                "NotionEventUpdaterService.UpdateEventsToInProgress (isMorning=false) --> Retrieving events from: {from} to: {to}", from,
                to);
            
            eventsToUpdate = await GetEvents(from, to);
        }

        return await notionService.UpdateEventsToInProgress(eventsToUpdate);
    }

    private async Task<PaginatedList<Page>> GetEvents(DateTime from, DateTime to)
    {
        var dateFilter = NotionEventRetrivalService.GetDateBetweenFilter("Date", from: from , to: to);
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = dateFilter,
            Sorts =
            [
                new Sort
                {
                    Direction = Direction.Ascending,
                    Property = "Date"
                }
            ]
        };
        var endsTodayEventList = await notionService.GetPaginatedList(databaseQuery);
        return endsTodayEventList;
    }

    public async Task<List<Page>> UpdateEventsToTrash()
    {
        var eventsToTrash = await GetCancelledEvents();
        return await notionService.DeleteEventsThatAreCancelled(eventsToTrash);
    }

    private async Task<PaginatedList<Page>> GetCancelledEvents()
    {
        var statusFilter = new StatusFilter("Status", "Cancelled");
        var databaseQuery = new DatabasesQueryParameters
        {
            Filter = statusFilter,
            Sorts =
            [
                new Sort
                {
                    Direction = Direction.Ascending,
                    Property = "Date"
                }
            ]
        };
        var events = await notionService.GetPaginatedList(databaseQuery);
        return events;
    }
}