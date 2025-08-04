using NotionReminderService.Models.Transport;

namespace NotionReminderService.Repositories.Transport;

public interface ITransportRepository
{
    Task UpdateBusStops(List<BusStop> busStops);
    Task<List<BusStop>> GetBusStops();
}