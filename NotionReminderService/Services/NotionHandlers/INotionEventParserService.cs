using Notion.Client;
using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Services.NotionHandlers;

public interface INotionEventParserService
{
    public Task<List<NotionEvent>> ParseEvent(bool isMorning);
    public Task<List<NotionEvent>> GetOngoingEvents();
    public Task<PaginatedList<Page>> GetPages(DateTime from, DateTime to);
    public CompoundFilter GetDateBetweenFilter(DateTime from, DateTime to);
}