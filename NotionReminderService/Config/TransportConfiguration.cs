namespace NotionReminderService.Config;

public class TransportConfiguration
{
    public required string DataMallUrl { get; set; }
    public required string AccountKey { get; set; }
    public required double Radius { get; set; }
}