namespace NotionReminderService.Models.NotionEvent;

public class NotionNewEvent
{
    public string? Name { get; set; }
    public string? Where { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? MiniReminderDesc { get; set; }
    public ReminderPeriodOptions? ReminderPeriod { get; set; }
}