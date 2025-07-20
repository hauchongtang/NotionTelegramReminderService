using NotionReminderService.Models.Agent;

namespace NotionReminderService.Api.PlanPulseAgent;

public interface IPlanPulseAgent
{
    public Task<AgentResponseModel> SendMessageAsync(AgentRequest request);
}