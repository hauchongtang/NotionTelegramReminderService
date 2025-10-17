using System.ComponentModel.DataAnnotations;

namespace NotionReminderService.Models.Weather.Rainfall;

public class RainfallIntensity
{
    [Key]
    public string Id { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
}