using Notion.Client;
using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Utils;

public abstract class NotionEventParser
{
    public static NotionEvent? GetNotionEvent(Page page)
    {
        var notionEventName = GetNotionEventName(page);
        var location = GetNotionEventLocation(page);
        var persons = GetNotionEventPerson(page);
        var status = GetNotionEventStatus(page);
        var tags = GetNotionEventTag(page);
        var date = GetNotionEventDate(page);
        var miniReminderDesc = GetNotionMiniReminderDescription(page);
        var miniReminderTrigger = GetNotionMiniReminderTrigger(page);
        var notionEvent = GenerateNotionEvent(page, date, notionEventName, location, persons, status, tags,
            miniReminderDesc, miniReminderTrigger);
        return notionEvent;
    }

    private static NotionEvent? GenerateNotionEvent(Page page, Date? date, string? notionEventName, string? location,
        string? persons, string? status, string? tags, string? miniReminderDesc, ReminderPeriodOptions? options)
    {
        if (date is null) return null;
        
        var notionEvent = new NotionEvent
        {
            Name = notionEventName,
            Where = location,
            Person = persons,
            Status = status,
            Tags = tags,
            Start = date.Start!.Value,
            End = date.End.HasValue ? date.End!.Value : null,
            Date = date.Start!.Value,
            Url = page.Url,
            MiniReminderDesc = miniReminderDesc,
            ReminderPeriod = options
        };

        return notionEvent;
    }

    private static Date? GetNotionEventDate(Page page)
    {
        if (!page.Properties.ContainsKey("Date")) return null;
        
        page.Properties.TryGetValue("Date", out var dateProperty);
        
        if (dateProperty is not { Type: PropertyValueType.Date }) return null;
        
        return ((DatePropertyValue) dateProperty).Date;
    }

    private static string? GetNotionEventLocation(Page page)
    {
        string? location = null;
        if (!page.Properties.ContainsKey("Where")) return location;
        page.Properties.TryGetValue("Where", out var where);
        var locRichText = ((RichTextPropertyValue) where!).RichText;
        location = locRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return location;
    }

    private static string? GetNotionEventName(Page page)
    {
        string? notionEventName = null;
        if (!page.Properties.ContainsKey("Name")) return notionEventName;
        page.Properties.TryGetValue("Name", out var title);
        var titleRichText = ((TitlePropertyValue) title!).Title;
        notionEventName = titleRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return notionEventName;
    }
    
    private static string? GetNotionEventPerson(Page page)
    {
        if (!page.Properties.ContainsKey("Person")) return null;
        
        page.Properties.TryGetValue("Person", out var personPropValue);
        var personList = ((PeoplePropertyValue) personPropValue!).People.Select(x => x.Name).ToList();
        return string.Join(", ", personList);
    }

    private static string? GetNotionEventStatus(Page page)
    {
        if (!page.Properties.ContainsKey("Status")) return null;

        page.Properties.TryGetValue("Status", out var statusPropValue);
        var status = ((StatusPropertyValue)statusPropValue!).Status;
        return status.Name;
    }

    private static string? GetNotionEventTag(Page page)
    {
        if (!page.Properties.ContainsKey("Tags")) return null;

        page.Properties.TryGetValue("Tags", out var multiSelectPropValue);
        var tags = ((MultiSelectPropertyValue)multiSelectPropValue!).MultiSelect.Select(x => x.Name);
        return string.Join(" | ", tags);
    }

    private static ReminderPeriodOptions? GetNotionMiniReminderTrigger(Page page)
    {
        if (!page.Properties.ContainsKey("Trigger Mini Reminder")) return null;

        page.Properties.TryGetValue("Trigger Mini Reminder", out var selectPropValue);
        var triggerProperty = ((SelectPropertyValue)selectPropValue!).Select?.Name;
        return triggerProperty switch
        {
            "On the day itself" => ReminderPeriodOptions.OnTheDayItself,
            "One day before" => ReminderPeriodOptions.OneDayBefore,
            "Two days before" => ReminderPeriodOptions.TwoDaysBefore,
            "One week before" => ReminderPeriodOptions.OneWeekBefore,
            _ => null
        };
    }

    private static string? GetNotionMiniReminderDescription(Page page)
    {
        if (!page.Properties.ContainsKey("Mini Reminder Description")) return null;

        page.Properties.TryGetValue("Mini Reminder Description", out var richTextPropValue);
        var description =
            ((RichTextPropertyValue)richTextPropValue!).RichText.Aggregate("", (s, rt) => s + rt.PlainText);
        return description;
    }
}