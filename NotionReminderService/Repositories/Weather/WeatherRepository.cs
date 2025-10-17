using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NotionReminderService.Models;
using NotionReminderService.Models.Weather;
using NotionReminderService.Models.Weather.Rainfall;
using NotionReminderService.Utils;

namespace NotionReminderService.Repositories.Weather;

public class WeatherRepository(DatabaseContext context, IDateTimeProvider dateTimeProvider) : IWeatherRepository
{
    public async Task UpdateRainfallStations(List<RainfallStationCached> stations)
    {
        await context.BulkInsertOrUpdateAsync(stations);
    }
    
    public async Task<Rainfall?> GetRainfallByDateTime(DateTime dateTime)
    {
        var utcDateTime = dateTime.ToUniversalTime().AddHours(8);
        return await context.Rainfalls.Where(x => x.Date == utcDateTime).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RainfallSlot>> GetRainFallSlots(string rainfallId, int slotNumber)
    {
        return await context.RainfallSlots
            .Where(x => x.RainfallId == rainfallId && x.SlotNumber == slotNumber)
            .ToListAsync();
    }

    public async Task<string?> CreateRainfall(DateTime dateTime)
    {
        var rainfall = new Rainfall
        {
            Date = dateTime.ToUniversalTime(),
            SlotsPerHour = 2,
            CreatedOn = dateTimeProvider.Now
        };
        var result = await context.Rainfalls.AddAsync(rainfall);
        await context.SaveChangesAsync();
        return result.Entity?.RainfallId;
    }
    
    public async Task UpsertRainfallSlots(List<RainfallSlot> readings)
    {
        await context.BulkInsertOrUpdateAsync(readings);
        await context.SaveChangesAsync();
    }

    public async Task RemoveRainfallSlots(int hourOfDay)
    {
        var currentDateTime = dateTimeProvider.Now;
        var rainfall = await GetRainfallByDateTime(currentDateTime.Date);
        if (rainfall is null)
        {
            return;
        }
        await context.RainfallSlots
            .Where(x => x.RainfallId == rainfall.RainfallId && x.HourOfDay <= hourOfDay)
            .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<RainfallSlot>?> GetRainfallSlotsLastHour()
    {
        var currentDateTime = dateTimeProvider.Now;
        var rainfall = await GetRainfallByDateTime(currentDateTime.Date);
        if (rainfall is null)
        {
            return null;
        }
        return await context.RainfallSlots
            .Where(x => 
                x.RainfallId == rainfall.RainfallId && 
                x.HourOfDay == currentDateTime.Hour)
            .ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetRainfallStationsFromList(List<string> stationIds)
    {
        return await context.RainfallStations
            .Where(x => stationIds.Contains(x.Id))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);
    }

    public async Task<RainfallIntensity?> GetRainfallIntensitySettings()
    {
        return await context.RainfallIntensities.FirstOrDefaultAsync();
    }
}