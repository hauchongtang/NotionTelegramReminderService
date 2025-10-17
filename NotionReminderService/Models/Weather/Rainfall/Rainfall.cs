using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace NotionReminderService.Models.Weather.Rainfall;

public class RainfallStationCached: BaseModel
{
    [Key]
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}

public class Rainfall: BaseModel
{
    [Key] 
    public string? RainfallId { get; set; }
    public DateTime Date { get; set; }
    public int SlotsPerHour { get; set; }
}

public class RainfallSlot: BaseModel
{
    [Key]
    public string? RainfallSlotId { get; set; }
    public required string? RainfallId { get; set; }
    public required string StationId { get; set; }
    public int HourOfDay { get; set; }
    public int SlotNumber { get; set; }
    public double RainfallAmount { get; set; }
}