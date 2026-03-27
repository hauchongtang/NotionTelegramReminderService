using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Utils;

public abstract class NotionEventDateFormatter
{
    public static string FormatEventDate(NotionEvent notionEvent, DateTime dateTimeNow)
    {
        if (notionEvent.Start is null) return string.Empty;
        var notionEventStart = notionEvent.Start.Value.AddHours(8);
        var notionEventEnd = notionEvent.End!.Value.AddHours(8);
        string eventDate;
        if (EventIsToday(notionEvent, dateTimeNow))
        {
            eventDate = notionEvent.End is null 
                ? notionEvent.IsWholeDayEvent 
                    ? "Today" 
                    : $"Today @ {notionEventStart:t}" 
                : notionEvent.IsWholeDayEvent 
                    ? $"Today \u2192 {notionEventEnd:F}" 
                    : $"Today @ {notionEventStart:t} \u2192 {notionEventEnd:F}";
        }
        else
        {
            eventDate = notionEvent.End is null
                ? notionEvent.IsWholeDayEvent 
                    ? $"{notionEventStart:D}" 
                    : $"{notionEventStart:F}"
                : notionEvent.IsWholeDayEvent 
                    ? $"{notionEventStart:D} \u2192 {notionEventEnd:D}"
                    : $"{notionEventStart:F} \u2192 {notionEventEnd:F}";
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