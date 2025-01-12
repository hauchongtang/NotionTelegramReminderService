using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NotionReminderService.Config;
using NotionReminderService.Models.GoogleAI;
using NotionReminderService.Utils;

namespace NotionReminderService.Api.GoogleAi;

public class GoogleAiApi(HttpClient httpClient, IOptions<GoogleAiConfiguration> googleAiConfig) : IGoogleAiApi
{
    public async Task<GeminiMessageResponse> GenerateContent(string prompt)
    {
        var data = BuildGeminiRequest(prompt);
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(
            new Uri($"{googleAiConfig.Value.Url}/{googleAiConfig.Value.ModelVersion}:generateContent?key={googleAiConfig.Value.ApiKey}"),
            content);
        response.EnsureSuccessStatusCode();

        return await ResponseHandler.HandleResponse<GeminiMessageResponse>(response);
    }
    
    public async Task<GeminiMessageResponse> GenerateContent(string prompt, GenerationConfig generationConfig)
    {
        var data = BuildGeminiRequest(prompt, generationConfig);
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(
            new Uri($"{googleAiConfig.Value.Url}/{googleAiConfig.Value.ModelVersion}:generateContent?key={googleAiConfig.Value.ApiKey}"),
            content);
        response.EnsureSuccessStatusCode();

        return await ResponseHandler.HandleResponse<GeminiMessageResponse>(response);
    }
    
    private static GeminiMessageRequest BuildGeminiRequest(
        string message,
        GenerationConfig? generationConfig = null,
        SafetySetting? safetySetting = null)
    {
        return new GeminiMessageRequest
        {
            Contents =
            [
                new Content
                {
                    Parts =
                    [
                        new Part
                        {
                            Text = message
                        }
                    ]
                }
            ],
            GenerationConfig = generationConfig,
            SafetySetting = safetySetting
        };
    }
}