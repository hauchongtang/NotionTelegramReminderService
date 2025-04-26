using NotionReminderService.Models.Transport;

namespace NotionReminderService.Services.BotHandlers.TransportHandler;

public interface ITransportService
{
    Task<List<BusArrival>?> GetNearestBusStops(double latitude, double longitude, double radius = 1.0);
}