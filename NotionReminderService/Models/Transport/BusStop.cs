using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace NotionReminderService.Models.Transport;

public class BusStop: BaseModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [JsonProperty(nameof(BusStopCode))]
    public int BusStopCode { get; set; }

    [JsonProperty(nameof(RoadName))]
    public string? RoadName { get; set; }

    [JsonProperty(nameof(Description))]
    public string? Description { get; set; }

    [JsonProperty(nameof(Latitude))]
    public double Latitude { get; set; }

    [JsonProperty(nameof(Longitude))]
    public double Longitude { get; set; }
}

public class BusStopsResponse: BaseModel
{
    [JsonProperty("value")]
    public List<BusStop>? Value { get; set; }
}