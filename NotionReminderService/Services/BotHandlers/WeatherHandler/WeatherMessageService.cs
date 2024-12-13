using Microsoft.Extensions.Options;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotionReminderService.Services.BotHandlers.WeatherHandler;

public class WeatherMessageService(
    IWeatherApi weatherApi,
    IGoogleAiApi googleAiApi,
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> botConfig,
    ILogger<IWeatherMessageService> logger) : IWeatherMessageService
{
    public async Task SendMessage(Chat? chat)
    {
        var weatherData = await weatherApi.GetRealTimeWeather();

        var forecasts = weatherData?[0].Forecasts;
        if (forecasts is null) return;
        
        var jsonString = "{ data: [";
        foreach (var areaForecast in forecasts)
        {
            jsonString += "{" + $"{areaForecast.Area} : {areaForecast.Forecast}" + "},";
        }
        jsonString += "]}";
        
        // API call to LLM Model to summarize weather forecast
        var weatherSummaryResponse = await googleAiApi.GenerateContent(
            "Summarise this weather forecast in 60 words like a weatherman with some creativity. " +
            "List locations with rainy conditions if any. " +
            "If majority of locations are rainy, then list only those dry locations: \n" +
            jsonString);
        var weatherText = weatherSummaryResponse.Candidates[0].Content.Parts[0].Text;
        var textToSend = $"""
                          {weatherText}
                          
                          <i>Powered by Gemini LLM agent using real time data from <a href="https://data.gov.sg">data.gov.sg</a></i>
                          Forcast for → {weatherData?[0].ValidPeriod.Text}
                          """;
        
        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData("Retrieve Again", "triggerForecast"));
        await botClient.SendMessage(chat ?? new ChatId(botConfig.Value.ChatId), textToSend, ParseMode.Html,
            replyMarkup: replyMarkup);
    }
}