using NotionReminderService.Api.Transport;
using NotionReminderService.Models.Transport;
using NotionReminderService.Repositories.Transport;
using NotionReminderService.Utils;

namespace NotionReminderService.Services.BotHandlers.TransportHandler;

public class TransportService(
    ILogger<TransportService> logger,
    ITransportRepository transportRepository,
    ITransportApi transportApi, IDateTimeProvider dateTimeProvider) : ITransportService
{
    public async Task UpdateBusStops()
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
        
        if (busStops.Count == 0) return;

        busStops.ForEach(b => b.UpdatedOn = dateTimeProvider.Now);
        await transportRepository.UpdateBusStops(busStops);
    }
    
    public async Task<List<BusArrival>?> GetNearestBusStops(double latitude, double longitude, double radius = 1.0,
        int page = 1)
    {
        var busStops = await transportRepository.GetBusStops();
        var filteredBusStops = busStops
            .Where(stop => LocationUtil.HaversineDistance(latitude, longitude, stop.Latitude, stop.Longitude) <= radius)
            .ToList();

        // Create a list of tasks: each getting bus arrival info and calculating the distance
        var tasks = filteredBusStops.Select(async stop =>
        {
            var busArrival = await transportApi.GetBusArrivalByBusStopCode(stop.BusStopCode);
            if (busArrival is not null)
            {
                busArrival.BusStopDescription = stop.Description;
                var distance = LocationUtil.HaversineDistance(latitude, longitude, stop.Latitude, stop.Longitude);
                return (Distance: distance, BusArrival: busArrival);
            }
            return (Distance: double.MaxValue, BusArrival: (BusArrival?)null);
        }).ToList();

        // Wait for all API calls to complete
        var results = await Task.WhenAll(tasks);

        // Filter out nulls and sort by distance
        var pageSize = 3;
        var nearestSorted = results
            .Where(x => x.BusArrival is not null && Math.Abs(x.Distance - double.MaxValue) > 0.01)
            .OrderBy(x => x.Distance)
            .Select(x => x.BusArrival!)
            .Skip((page-1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return nearestSorted;
    }
}