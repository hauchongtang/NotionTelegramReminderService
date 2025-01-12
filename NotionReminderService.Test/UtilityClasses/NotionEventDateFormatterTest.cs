using NotionReminderService.Test.TestUtils;
using NotionReminderService.Utils;

namespace NotionReminderService.Test.UtilityClasses;

public class NotionEventDateFormatterTest
{
    [Test]
    public void FormatEventDate_EventDateIsToday_ReturnsToday()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
            .WithStartDate(new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build())
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);

        Assert.That(formattedResult, Is.EqualTo("Today"));
    }
    
    [Test]
    public void FormatEventDate_EventDateIsTodayWithStartTime_ReturnsTodayWithStartTime()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).WithHour(6).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);

        Assert.That(formattedResult, Is.EqualTo($"Today @ {startDate:t}"));
    }

    [Test]
    public void FormatEventDate_EventDateIsTodayWithDateEndTime_ReturnsTodayWithEndTime()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var endDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).WithHour(7).WithMinute(30).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithEndDate(endDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);

        Assert.That(formattedResult, Is.EqualTo($"Today \u2192 {endDate:F}"));
    }

    [Test]
    public void FormatEventDate_EventDateIsTodayWithBothDateStartEndTime_ReturnsTodayWithStartEndTime()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).WithHour(6).Build();
        var endDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).WithHour(7).WithMinute(30).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithEndDate(endDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);

        Assert.That(formattedResult, Is.EqualTo($"Today @ {startDate:t} \u2192 {endDate:F}"));
    }

    [Test]
    public void FormatEventDate_EventDateNotTodayAndIsWholeDayEvent_ReturnsLongDate()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);
        
        Assert.That(formattedResult, Is.EqualTo($"{startDate:D}"));
    }

    [Test]
    public void FormatEventDate_EventDateNotTodayWithOneDayEventStartTimeNoEndTime_ReturnsFullDateLong()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(6).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);
        
        Assert.That(formattedResult, Is.EqualTo($"{startDate:F}"));
    }

    [Test]
    public void FormatEventDate_EventDateNotTodayWithStartEndDateNoTime_ReturnsStartToEnd()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).Build();
        var endDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(13).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithEndDate(endDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);
        
        Assert.That(formattedResult, Is.EqualTo($"{startDate:D} \u2192 {endDate:D}"));
    }

    [Test]
    public void FormatEventDate_EventDateNotTodayWithStartEndDateWithTime_ReturnsStartToEndWithTime()
    {
        var currentDt = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(10).Build();
        var startDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(12).WithHour(1).Build();
        var endDate = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(13).WithHour(6).Build();
        var notionEventToday = new NotionEventBuilder()
            .WithName("Event 1")
            .WithLocation("KL")
            .WithPerson("Person1, Person2")
            .WithStatus("Status 1")
            .WithTags("Tag 1")
            .WithDate(startDate)
            .WithStartDate(startDate)
            .WithEndDate(endDate)
            .WithUrl("www.123123123123.org")
            .Build();

        var formattedResult = NotionEventDateFormatter.FormatEventDate(notionEventToday, currentDt);
        
        Assert.That(formattedResult, Is.EqualTo($"{startDate:F} \u2192 {endDate:F}"));
    }
}