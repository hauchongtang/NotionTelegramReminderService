using NotionReminderService.Models.NotionEvent;

namespace NotionReminderService.Test.TestUtils;

public class NotionEventBuilder
{
    private string? _name;
    private string? _location;
    private string? _person;
    private string? _tag;
    private string? _status;
    private DateTime? _date;
    private DateTime? _start;
    private DateTime? _end;
    private string? _url;

    public NotionEventBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public NotionEventBuilder WithLocation(string location)
    {
        _location = location;
        return this;
    }

    public NotionEventBuilder WithPerson(string person)
    {
        _person = person;
        return this;
    }

    public NotionEventBuilder WithTags(string tag)
    {
        _tag = tag;
        return this;
    }

    public NotionEventBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }

    public NotionEventBuilder WithDate(DateTime dt)
    {
        _date = dt;
        return this;
    }

    public NotionEventBuilder WithStartDate(DateTime dt)
    {
        _start = dt;
        return this;
    }

    public NotionEventBuilder WithEndDate(DateTime dt)
    {
        _end = dt;
        return this;
    }

    public NotionEventBuilder WithUrl(string? url)
    {
        _url = url;
        return this;
    }
    
    public NotionEvent Build()
    {
        return new NotionEvent
        {
            Name = _name,
            Where = _location,
            Person = _person,
            Tags = _tag,
            Status = _status,
            Date = _date,
            Start = _start,
            End = _end,
            Url = _url
        };
    }
}