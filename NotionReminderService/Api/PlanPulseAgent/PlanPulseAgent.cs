using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NotionReminderService.Config;
using NotionReminderService.Models.Agent;
using NotionReminderService.Utils;

namespace NotionReminderService.Api.PlanPulseAgent;

public class PlanPulseAgent(IHttpClientFactory httpClientFactory, IOptions<PlanPulseAgentConfiguration> config) : IPlanPulseAgent
{
    public async Task<AgentResponseModel> SendMessageAsync(AgentRequest request)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Secret-Key",  config.Value.SecretKey);
        try
        {
            var requestUri = $"{config.Value.Url}/v1/telegram/";
            var content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();
            return await ResponseHandler.HandleResponse<AgentResponseModel>(response);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}