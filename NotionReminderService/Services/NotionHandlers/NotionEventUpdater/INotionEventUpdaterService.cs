using Notion.Client;

namespace NotionReminderService.Services.NotionHandlers.NotionEventUpdater;

public interface INotionEventUpdaterService
{
    public Task<List<Page>> UpdateEventsToCompleted();
    public Task<List<Page>> UpdateEventsToInProgress(bool isMorningJob);
    public Task UpdateEventsToTrash()
}