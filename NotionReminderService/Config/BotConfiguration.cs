namespace NotionReminderService.Config;

public class BotConfiguration
{
    public required string BotToken { get; set; }
    public required Uri BotWebhookUrl { get; set; }
    public required string SecretToken { get; set; }
    public required string ChatId { get; set; }
}