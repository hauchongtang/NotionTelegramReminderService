using Notion.Client;

namespace NotionReminderService.Services.NotionHandlers.NotionService;

public interface INotionService
{
    public Task<PaginatedList<Page>> GetPaginatedList(DatabasesQueryParameters parameters);
    public Task<List<Page>> UpdateEventsToCompleted(PaginatedList<Page> pages);
    public Task<List<Page>> UpdateEventsToInProgress(PaginatedList<Page> pages);
}