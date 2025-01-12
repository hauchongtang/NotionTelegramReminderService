using NotionReminderService.Models.GoogleAI;

namespace NotionReminderService.Api.GoogleAi;

public interface IGoogleAiApi
{
    public Task<GeminiMessageResponse> GenerateContent(string prompt);
    public Task<GeminiMessageResponse> GenerateContent(string prompt, GenerationConfig generationConfig);
}