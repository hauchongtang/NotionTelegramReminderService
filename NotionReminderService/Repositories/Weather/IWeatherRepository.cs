using NotionReminderService.Models.Weather;
using NotionReminderService.Models.Weather.Rainfall;

namespace NotionReminderService.Repositories.Weather;

public interface IWeatherRepository
{
    Task UpdateRainfallStations(List<RainfallStationCached> stations);
    Task<Rainfall?> GetRainfallByDateTime(DateTime dateTime);
    Task<string?> CreateRainfall(DateTime dateTime);
    Task UpsertRainfallSlots(List<RainfallSlot> readings);
    Task<IEnumerable<RainfallSlot>> GetRainFallSlots(string rainfallId, int slotNumber, int hourOfDay);
    Task RemoveRainfallSlots(int hourOfDay);
    Task<IEnumerable<RainfallSlot>?> GetRainfallSlotsLastHour();
    Task<Dictionary<string, string>> GetRainfallStationsFromList(List<string> stationIds);
    Task<RainfallIntensity?> GetRainfallIntensitySettings();
}