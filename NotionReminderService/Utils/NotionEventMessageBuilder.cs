using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Utils;

public class NotionEventMessageBuilder
{
    private NotionEvent _notionEvent;
    private DateTime _dateTimeNow;

    public NotionEventMessageBuilder WithNotionEvent(NotionEvent notionEvent, DateTime dateTimeNow)
    {
        _notionEvent = notionEvent;
        _dateTimeNow = dateTimeNow;
        return this;
    }

    public string Build()
    {
        if (_notionEvent is null) throw new Exception("NotionEvent is null");
        
        return $"""

             <b>🌟 <a href="{_notionEvent.Url}">{_notionEvent.Name}</a></b>
             <b>📍 {_notionEvent.Where}</b>
             <b>👥 {_notionEvent.Person}</b>
             <b>▶️ {_notionEvent.Status}</b>
             <b>🏷️ {_notionEvent.Tags}</b>
             <b>📅 {NotionEventDateFormatter.FormatEventDate(_notionEvent, _dateTimeNow)}</b>

             """;
    }
}