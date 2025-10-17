using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NotionReminderService.Models.Weather;

public class RainfallResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("errorMsg")]
    public string ErrorMsg { get; set; }

    [JsonProperty("data")]
    public RainfallData Data { get; set; }
}

public class RainfallData
{
    [JsonProperty("stations")]
    public List<RainfallStation> Stations { get; set; }

    [JsonProperty("readings")]
    public List<RainfallReading> Readings { get; set; }

    [JsonProperty("readingType")]
    public string ReadingType { get; set; }

    [JsonProperty("readingUnit")]
    public string ReadingUnit { get; set; }

    [JsonProperty("paginationToken")]
    public string PaginationToken { get; set; }
}

public class RainfallStation
{
    [Key]
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("location")]
    public RainfallLabelLocation LabelLocation { get; set; }
}

public class RainfallReading
{
    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("data")]
    public List<StationReading> Data { get; set; }
}

public class StationReading
{
    [JsonProperty("stationId")]
    public string StationId { get; set; }

    [JsonProperty("value")]
    public double Value { get; set; }
}

public class RainfallLabelLocation
{
    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}
