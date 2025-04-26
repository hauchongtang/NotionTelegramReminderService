namespace NotionReminderService.Models.Transport;

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class BusArrival
{
    [JsonProperty(nameof(BusStopCode))]
    public int BusStopCode { get; set; }
    [JsonProperty(nameof(Services))]
    public List<BusService>? Services { get; set; }
    public string? BusStopDescription { get; set; }
}

public class BusService
{
    [JsonProperty(nameof(ServiceNo))]
    public string? ServiceNo { get; set; }

    [JsonProperty(nameof(Operator))]
    public string? Operator { get; set; }

    [JsonProperty(nameof(NextBus))]
    public NextBus? NextBus { get; set; }
    [JsonProperty(nameof(NextBus1))]
    public NextBus? NextBus1 { get; set; }
    [JsonProperty(nameof(NextBus2))]
    public NextBus? NextBus2 { get; set; }
    [JsonProperty(nameof(NextBus3))]
    public NextBus? NextBus3 { get; set; }
}

public class NextBus
{
    [JsonProperty(nameof(OriginCode))]
    public string? OriginCode { get; set; }

    [JsonProperty(nameof(DestinationCode))]
    public string? DestinationCode { get; set; }

    [JsonProperty(nameof(EstimatedArrival))]
    public DateTime? EstimatedArrival { get; set; }

    [JsonProperty(nameof(Monitored))]
    public int Monitored { get; set; }

    [JsonProperty(nameof(Latitude))]
    public string? Latitude { get; set; }

    [JsonProperty(nameof(Longitude))]
    public string? Longitude { get; set; }

    [JsonProperty(nameof(VisitNumber))]
    public string? VisitNumber { get; set; }

    [JsonProperty(nameof(Load))]
    public string? Load { get; set; }

    [JsonProperty(nameof(Feature))]
    public string? Feature { get; set; }

    [JsonProperty(nameof(Type))]
    public string? Type { get; set; }
}
