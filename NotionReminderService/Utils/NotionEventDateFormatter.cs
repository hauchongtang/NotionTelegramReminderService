using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Utils;

public abstract class NotionEventDateFormatter
{
    public static string FormatEventDate(NotionEvent notionEvent, DateTime dateTimeNow)
    {
        if (notionEvent.Start is null) return string.Empty;
        
        string eventDate;
        if (EventIsToday(notionEvent, dateTimeNow))
        {
            eventDate = notionEvent.End is null 
                ? notionEvent.IsWholeDayEvent 
                    ? "Today" 
                    : $"Today @ {notionEvent.Start.Value:t}" 
                : notionEvent.IsWholeDayEvent 
                    ? $"Today \u2192 {notionEvent.End.Value:F}" 
                    : $"Today @ {notionEvent.Start.Value:t} \u2192 {notionEvent.End.Value:F}";
        }
        else
        {
            eventDate = notionEvent.End is null
                ? notionEvent.IsWholeDayEvent 
                    ? $"{notionEvent.Start.Value:D}" 
                    : $"{notionEvent.Start.Value:F}"
                : notionEvent.IsWholeDayEvent 
                    ? $"{notionEvent.Start.Value:D} \u2192 {notionEvent.End.Value:D}"
                    : $"{notionEvent.Start.Value:F} \u2192 {notionEvent.End.Value:F}";
        }

        return eventDate;
    }

    private static bool EventIsToday(NotionEvent notionEvent, DateTime dateTimeNow)
    {
        return notionEvent.Start != null
               && notionEvent.Start.Value.Year == dateTimeNow.Year
               && notionEvent.Start.Value.Month == dateTimeNow.Month
               && notionEvent.Start.Value.Day == dateTimeNow.Day;
    }
}