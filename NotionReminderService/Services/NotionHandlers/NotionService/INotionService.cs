using Notion.Client;
using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Services.NotionHandlers.NotionService;

public interface INotionService
{
    public Task<PaginatedList<Page>> GetPaginatedList(DatabasesQueryParameters parameters);
    public Task<Page?> GetPageFromDatabase(DatabasesQueryParameters parameters);
    public Task<List<Page>> UpdateEventsToCompleted(PaginatedList<Page> pages);
    public Task<List<Page>> UpdateEventsToInProgress(PaginatedList<Page> pages);
    public Task<Page> CreateNewEvent(PagesCreateParameters parameters);
    public Task<List<Page>> DeleteEventsThatAreCancelled(PaginatedList<Page> pagesToDelete);
    public Task<Page> UpdatePageTag(string pageId, string tagId);
}