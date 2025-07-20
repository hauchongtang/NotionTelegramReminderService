using Newtonsoft.Json;

namespace NotionReminderService.Models.Agent;

public class AgentResponseModel
{
    [JsonProperty("chat_id")]
    public string ChatId { get; set; }

    [JsonProperty("chat_message")]
    public string ChatMessage { get; set; }

    [JsonProperty("response")]
    public string Response { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("metadata")]
    public AgentMetadata Metadata { get; set; }
}

public class AgentMetadata
{
    [JsonProperty("selected_agent")]
    public string SelectedAgent { get; set; }

    [JsonProperty("confidence_scores")]
    public Dictionary<string, double> ConfidenceScores { get; set; }

    [JsonProperty("reasoning")]
    public string Reasoning { get; set; }

    [JsonProperty("agent_response")]
    public AgentInnerResponse AgentResponse { get; set; }
}

public class AgentInnerResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("response")]
    public string Response { get; set; }

    [JsonProperty("agent")]
    public string Agent { get; set; }

    [JsonProperty("tools_available")]
    public List<string> ToolsAvailable { get; set; }

    [JsonProperty("message_count")]
    public int MessageCount { get; set; }
}