using NotionReminderService.Models.Weather;
using NotionReminderService.Models.Weather.Rainfall;
using NotionReminderService.Test.TestData;

namespace NotionReminderService.Test.TestUtils.Rainfall;

public class RainfallSlotBuilder
{
    private string RainfallId { get; set; }
    private int HourOfDay { get; set; }
    private string LastTimeStamp { get; set; }
    
    public RainfallSlotBuilder WithRainfallId(string rainfallId)
    {
        RainfallId = rainfallId;
        return this;
    }

    public RainfallSlotBuilder WithHourOfDay(int hourOfDay)
    {
        HourOfDay = hourOfDay;
        return this;
    }
    
    public RainfallSlotBuilder WithLastTimeStamp(string lastTimeStamp)
    {
        LastTimeStamp = lastTimeStamp;
        return this;
    }

    private List<RainfallSlot> GetRainfallReadingsFromFile()
    {
        var rainfallResponse =
            TestDataUtils.LoadTestDataFromFile<RainfallResponse>(TestDataUtils.NoRainfallResponseFilePath);
        var rainfallSlots = rainfallResponse.Data.Readings[0].Data.Select(r => new RainfallSlot
        {
            RainfallId = RainfallId,
            StationId = r.StationId,
            HourOfDay = HourOfDay,
            LastTimeStamp = LastTimeStamp,
            RainfallAmount = r.Value
        }).ToList();
        return rainfallSlots;
    }

    public List<RainfallSlot> Build()
    {
        return GetRainfallReadingsFromFile();
    }
}