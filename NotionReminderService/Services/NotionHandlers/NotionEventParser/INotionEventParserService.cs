using Notion.Client;
using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Services.NotionHandlers.NotionEventParser;

public interface INotionEventParserService
{
    public Task<List<NotionEvent>> ParseEvent(bool isMorning);
    public Task<List<NotionEvent>> GetOngoingEvents();
    public bool IsEventStillOngoing(NotionEvent e);
    public Task<List<NotionEvent>> GetMiniReminders();
}