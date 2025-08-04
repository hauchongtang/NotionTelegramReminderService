using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NotionReminderService.Models;
using NotionReminderService.Models.Transport;

namespace NotionReminderService.Repositories.Transport;

public class TransportRepository(DatabaseContext context) : ITransportRepository
{
    public async Task<List<BusStop>> GetBusStops()
    {
        var result = await context.BusStops.ToListAsync();
        return result;
    }

    public async Task UpdateBusStops(List<BusStop> busStops)
    {
        await context.BulkInsertOrUpdateAsync(busStops);
    }
}