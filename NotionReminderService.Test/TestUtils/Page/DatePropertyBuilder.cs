using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class DatePropertyBuilder
{
    private DateTime _start;
    private DateTime _end;

    public DatePropertyBuilder WithStartDt(DateTime start)
    {
        _start = start;
        return this;
    }

    public DatePropertyBuilder WithEndDt(DateTime end)
    {
        _end = end;
        return this;
    }
    
    public DatePropertyValue Build()
    {
        var date = new Date
        {
            Start = _start,
            End = _end
        };
        return new DatePropertyValue
        {
            Date = date
        };
    }
}