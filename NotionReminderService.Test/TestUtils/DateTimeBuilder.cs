namespace NotionReminderService.Test.TestUtils;

public class DateTimeBuilder
{
    private int _day = 1;
    private int _month = 1;
    private int _year = 2000;
    private int _hour = 1;
    private int _minute = 0;

    public DateTimeBuilder WithYear(int year)
    {
        _year = year;
        return this;
    }

    public DateTimeBuilder WithMonth(int month)
    {
        _month = month;
        return this;
    }

    public DateTimeBuilder WithDay(int day)
    {
        _day = day;
        return this;
    }

    public DateTimeBuilder WithHour(int hour)
    {
        _hour = hour;
        return this;
    }

    public DateTimeBuilder WithMinute(int minute)
    {
        _minute = minute;
        return this;
    }

    public DateTime Build()
    {
        return new DateTime(_year, _month, _day, _hour, _minute, 0);
    }
}