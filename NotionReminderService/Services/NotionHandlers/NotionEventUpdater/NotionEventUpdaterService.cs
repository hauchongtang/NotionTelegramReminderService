using Notion.Client;
using NotionReminderService.Services.NotionHandlers.NotionEventParser;
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
        var dateFilter = NotionEventParserService.GetDateBetweenFilter(from: dateTimeAtStartOfDay, to: dateTimeAtEndOfDay);
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
        return endsTodayEventList;
    }

    public async Task<List<Page>> UpdateEventsToInProgress(bool isMorningJob)
    {
        PaginatedList<Page> eventsToUpdate;
        if (isMorningJob)
        {
            eventsToUpdate = await GetEvents(dateTime.Now.Date, dateTime.Now.Date.AddHours(12));
        }
        else
        {
            eventsToUpdate = await GetEvents(dateTime.Now.Date.AddHours(12).AddMinutes(1),
                dateTime.Now.Date.AddHours(23).AddMinutes(59));
        }

        return await notionService.UpdateEventsToInProgress(eventsToUpdate);
    }

    private async Task<PaginatedList<Page>> GetEvents(DateTime from, DateTime to)
    {
        var dateFilter = NotionEventParserService.GetDateBetweenFilter(from: from , to: to);
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
}