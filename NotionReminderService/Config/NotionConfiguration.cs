namespace NotionReminderService.Config;

public class NotionConfiguration
{
    public required string NotionAuthToken { get; set; }
    public required string DatabaseId { get; set; }
}