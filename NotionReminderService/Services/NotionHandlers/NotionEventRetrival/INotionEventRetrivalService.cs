using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Services.NotionHandlers.NotionEventRetrival;

public interface INotionEventRetrivalService
{
    public Task<List<NotionEvent>> GetNotionEvents(bool isMorning);
    public Task<List<NotionEvent>> GetOngoingEvents();
    public bool IsEventStillOngoing(NotionEvent e);
    public Task<List<NotionEvent>> GetMiniReminders();
}