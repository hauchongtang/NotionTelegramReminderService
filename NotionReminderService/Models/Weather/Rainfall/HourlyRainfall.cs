namespace NotionReminderService.Models.Weather.Rainfall;

public class HourlyRainfall
{
    public string StationId { get; set; }
    public string Location { get; set; }
    public double Amount { get; set; }
    public int Intensity { get; set; }
}