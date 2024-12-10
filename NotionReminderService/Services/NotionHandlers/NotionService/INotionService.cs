using Notion.Client;

namespace NotionReminderService.Services.NotionHandlers.NotionService;

public interface INotionService
{
    public Task<PaginatedList<Page>> GetPaginatedList(DatabasesQueryParameters parameters);
}