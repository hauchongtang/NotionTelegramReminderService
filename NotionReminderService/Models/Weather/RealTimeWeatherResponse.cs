using Newtonsoft.Json;

namespace NotionReminderService.Models.Weather;

public class RealTimeWeatherResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("data")]
    public WeatherData Data { get; set; }

    [JsonProperty("errorMsg")]
    public string ErrorMessage { get; set; }
}

public class WeatherData
{
    [JsonProperty("area_metadata")]
    public List<AreaMetadata> AreaMetadata { get; set; }

    [JsonProperty("items")]
    public List<WeatherItem>? Items { get; set; }
}

public class AreaMetadata
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("label_location")]
    public LabelLocation LabelLocation { get; set; }
}

public class LabelLocation
{
    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}

public class WeatherItem
{
    [JsonProperty("update_timestamp")]
    public string UpdateTimestamp { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("valid_period")]
    public ValidPeriod ValidPeriod { get; set; }

    [JsonProperty("forecasts")]
    public List<AreaForecast> Forecasts { get; set; }
}

public class ValidPeriod
{
    [JsonProperty("start")]
    public string Start { get; set; }

    [JsonProperty("end")]
    public string End { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }
}

public class AreaForecast
{
    [JsonProperty("area")]
    public string Area { get; set; }

    [JsonProperty("forecast")]
    public string Forecast { get; set; }
}