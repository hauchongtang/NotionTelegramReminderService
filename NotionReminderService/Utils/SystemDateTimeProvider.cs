namespace NotionReminderService.Utils;

public interface IDateTimeProvider
{
    DateTime Now { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.UtcNow.AddHours(8);
}