using Newtonsoft.Json;

namespace NotionReminderService.Models.Agent;

public class AgentRequest
{
    [JsonProperty("chat_id")]
    public string ChatId { get; set; }
    [JsonProperty("chat_message")]
    public string ChatMessage { get; set; }

    public AgentRequest(string chatId, string chatMessage)
    {
        if (string.IsNullOrWhiteSpace(chatId))
        {
            throw new ArgumentException("Chat ID cannot be null or empty.");
        }
        if (chatMessage.Length < 10)
        {
            throw new ArgumentException("Chat message must be at least 10 characters long.");
        }
        ChatId = chatId;
        ChatMessage = chatMessage;
    }
}