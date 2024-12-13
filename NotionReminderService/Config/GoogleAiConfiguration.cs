namespace NotionReminderService.Config;

public class GoogleAiConfiguration
{
    public required string Url { get; set; }
    public required string ApiKey { get; set; }
    public required string ModelVersion { get; set; }
}