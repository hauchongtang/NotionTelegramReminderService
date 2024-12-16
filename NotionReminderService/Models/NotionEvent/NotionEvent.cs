namespace NotionReminderService.Models.NotionEvent;

public class NotionEvent
{
    public required string? Name { get; set; }
    public required string? Where { get; set; }
    public required string? Person { get; set; }
    public required string? Tags { get; set; }
    public required string? Status { get; set; }
    public DateTime? Date { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public bool IsWholeDayEvent => Start is { Hour: 0, Minute: 0, Second: 0 };
    public required string? Url { get; set; }
    public string? MiniReminderDesc { get; set; }
    public ReminderPeriodOptions? ReminderPeriod { get; set; }
}

public enum ReminderPeriodOptions
{
    OnTheDayItself = 0,
    OneDayBefore = 1,
    TwoDaysBefore = 2,
    OneWeekBefore = 7
}