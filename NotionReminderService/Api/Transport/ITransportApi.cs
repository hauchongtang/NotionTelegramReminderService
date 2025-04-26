using NotionReminderService.Models.Transport;

namespace NotionReminderService.Api.Transport;

public interface ITransportApi
{
    Task<List<BusStop>?> GetBusStops(int page, int pageSize);
    Task<BusArrival?> GetBusArrivalByBusStopCode(int busStopCode);
}