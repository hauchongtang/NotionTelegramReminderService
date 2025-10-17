namespace NotionReminderService.Models.Weather.Rainfall;

public class RainfallSummary
{
    public List<string> NoRainfallStations { get; set; } = new();
    public List<string> LightRainfallStations { get; set; } = new();
    public List<string> ModerateRainfallStations { get; set; } = new();
    public List<string> HeavyRainfallStations { get; set; } = new();
}