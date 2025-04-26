using NotionReminderService.Api.Transport;
using NotionReminderService.Models.Transport;
using NotionReminderService.Utils;

namespace NotionReminderService.Services.BotHandlers.TransportHandler;

public class TransportService(ITransportApi transportApi) : ITransportService
{
    public async Task<List<BusArrival>?> GetNearestBusStops(double latitude, double longitude, double radius = 1.0)
    {
        var page = 1;
        var busStops = new List<BusStop>();
        while (true)
        {
            var busStopsPage = await transportApi.GetBusStops(page, 500);
            if (busStopsPage is null || busStopsPage.Count == 0) break;
            busStops.AddRange(busStopsPage);
            page++;
        }
        
        if (busStops.Count == 0) return null;
        
        var nearestBusStops = new SortedList<double, BusArrival>();
        foreach (var stop in busStops)
        {
            var distance = 
                LocationUtil.HaversineDistance(latitude, longitude, stop.Latitude, stop.Longitude);
            if (distance <= radius)
            {
                var busArrival = await transportApi.GetBusArrivalByBusStopCode(stop.BusStopCode);
                if (busArrival is not null)
                {
                    busArrival.BusStopDescription = stop.Description;
                    nearestBusStops.Add(distance, busArrival);
                }
            }
        }
        return nearestBusStops.Values.ToList();
    }
}