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
        var dateProperty = PropertyValueParser<DatePropertyValue>.GetValueFromPage(page, "Date");
        return dateProperty!.Date;
    }

    private static string? GetNotionEventLocation(Page page)
    {
        string? location = null;
        var where = PropertyValueParser<RichTextPropertyValue>.GetValueFromPage(page, "Where");
        var locRichText = where!.RichText;
        location = locRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return location;
    }

    private static string? GetNotionEventName(Page page)
    {
        string? notionEventName = null;
        var title = PropertyValueParser<TitlePropertyValue>.GetValueFromPage(page, "Name");
        var titleRichText = title!.Title;
        notionEventName = titleRichText.Aggregate("", (s, rt) => s + rt.PlainText);

        return notionEventName;
    }
    
    private static string? GetNotionEventPerson(Page page)
    {
        var personPropValue = PropertyValueParser<PeoplePropertyValue>.GetValueFromPage(page, "Person");
        var personList = personPropValue!.People.Select(x => x.Name).ToList();
        return string.Join(", ", personList);
    }

    private static string? GetNotionEventStatus(Page page)
    {
        var statusPropValue = PropertyValueParser<StatusPropertyValue>.GetValueFromPage(page, "Status");
        var status = statusPropValue!.Status;
        return status.Name;
    }

    // TODO: Refactor all GetProperty methods. Remove hardcoding of keys.
    private static string? GetNotionEventTag(Page page)
    {
        var multiSelectPropValue = PropertyValueParser<MultiSelectPropertyValue>.GetValueFromPage(page, "Tags");
        var tags = multiSelectPropValue.MultiSelect!.Select(x => x.Name);
        return string.Join(" | ", tags);
    }

    private static ReminderPeriodOptions? GetNotionMiniReminderTrigger(Page page)
    {
        var selectPropValue = PropertyValueParser<SelectPropertyValue>.GetValueFromPage(page, "Trigger Mini Reminder");
        var triggerProperty = selectPropValue!.Select?.Name;
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
        var richTextPropValue = PropertyValueParser<RichTextPropertyValue>.GetValueFromPage(page, "Mini Reminder Description");
        var description =
            richTextPropValue!.RichText.Aggregate("", (s, rt) => s + rt.PlainText);
        return description;
    }
}