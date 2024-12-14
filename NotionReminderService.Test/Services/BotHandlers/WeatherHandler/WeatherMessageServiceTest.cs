using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.Models.GoogleAI;
using NotionReminderService.Models.Weather;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace NotionReminderService.Test.Services.BotHandlers.WeatherHandler;

public class WeatherMessageServiceTest
{
    private Mock<IWeatherApi> _weatherApi;
    private Mock<IGoogleAiApi> _googleAiApi;
    private Mock<ITelegramBotClient> _botClient;
    private Mock<IOptions<BotConfiguration>> _botConfig;
    private Mock<ILogger<IWeatherMessageService>> _logger;
    private WeatherMessageService _weatherMessageService;

    [SetUp]
    public void SetUp()
    {
        _weatherApi = new Mock<IWeatherApi>();
        _googleAiApi = new Mock<IGoogleAiApi>();
        _botClient = new Mock<ITelegramBotClient>();
        _botConfig = new Mock<IOptions<BotConfiguration>>();
        _logger = new Mock<ILogger<IWeatherMessageService>>();
        _weatherMessageService = new WeatherMessageService(_weatherApi.Object, _googleAiApi.Object, _botClient.Object,
            _botConfig.Object, _logger.Object);
    }

    [Test]
    public async Task SendMessage_NoForecasts_MessageNotSent()
    {
        _weatherApi.Setup(x => x.GetRealTimeWeather()).ReturnsAsync(new List<WeatherItem>
        {
            new()
        });

        await _weatherMessageService.SendMessage(It.IsAny<Chat?>());
        
        _googleAiApi.Verify(x => x.GenerateContent(It.IsAny<string>()), Times.Never);
        _botClient.Verify(x => x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SendMessage_HaveForecastsHavePromptResponse_MessageSent()
    {
        _weatherApi.Setup(x => x.GetRealTimeWeather()).ReturnsAsync(new List<WeatherItem>
        {
            new()
            {
                ValidPeriod = new ValidPeriod
                {
                    Text = "3 pm to 5 pm"
                },
                Forecasts =
                [
                    new AreaForecast
                    {
                        Area = "Jurong",
                        Forecast = "Cloudy"
                    }
                ]
            }
        });
        _googleAiApi.Setup(x => x.GenerateContent(It.IsAny<string>())).ReturnsAsync(new GeminiMessageResponse
        {
            Candidates =
            [
                new Candidate
                {
                    Content = new Content
                    {
                        Parts =
                        [
                            new Part
                            {
                                InlineData = new InlineData
                                {
                                    Data = "Weather is cloudy today."
                                },
                            }
                        ]
                    }
                }
            ],
            PromptFeedback = new PromptFeedback()
        });
        _botClient.Setup(x => x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        await _weatherMessageService.SendMessage(new Chat { Id = 123 });
    }
}