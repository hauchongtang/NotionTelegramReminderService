using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using NotionReminderService.Config;
using NotionReminderService.Models.Transport;
using NotionReminderService.Utils;

namespace NotionReminderService.Api.Transport;

public class TransportApi(IHttpClientFactory httpClientFactory, IOptions<TransportConfiguration> config) : ITransportApi
{
    public async Task<List<BusStop>?> GetBusStops(int page, int pageSize)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("AccountKey", config.Value.AccountKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync($"https://datamall2.mytransport.sg/ltaodataservice/BusStops?$skip={page * pageSize}");
        response.EnsureSuccessStatusCode();
        var busStopsResponse = await ResponseHandler.HandleResponse<BusStopsResponse>(response);
        return busStopsResponse.Value;
    }
    
    public async Task<BusArrival?> GetBusArrivalByBusStopCode(int busStopCode)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("AccountKey", config.Value.AccountKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await client.GetAsync($"https://datamall2.mytransport.sg/ltaodataservice/v3/BusArrival?BusStopCode={busStopCode}");
            response.EnsureSuccessStatusCode();
            return await ResponseHandler.HandleResponse<BusArrival>(response);
        }
        catch (Exception e)
        {
            return null;
        }
    }
}