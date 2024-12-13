using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using Telegram.Bot;

namespace NotionReminderService.Test.Services.BotHandlers.WeatherHandler;

public class WeatherMessageServiceTest
{
    private Mock<IWeatherApi> _weatherApi;
    private Mock<IGoogleAiApi> _googleAiApi;
    private Mock<ITelegramBotClient> _botClient;
    private Mock<IOptions<BotConfiguration>> _botConfig;
    private Mock<ILogger<IWeatherMessageService>> _logger;

    [SetUp]
    public void SetUp()
    {
        _weatherApi = new Mock<IWeatherApi>();
        _googleAiApi = new Mock<IGoogleAiApi>();
        _botClient = new Mock<ITelegramBotClient>();
        _botConfig = new Mock<IOptions<BotConfiguration>>();
        _logger = new Mock<ILogger<IWeatherMessageService>>();
    }

    [Test]
    public void SendMessage_NoForecasts_MessageNotSent()
    {
        
    }
}