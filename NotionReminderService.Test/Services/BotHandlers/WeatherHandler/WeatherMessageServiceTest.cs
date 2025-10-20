using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.Models.GoogleAI;
using NotionReminderService.Models.Weather;
using NotionReminderService.Models.Weather.Rainfall;
using NotionReminderService.Repositories.Weather;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Test.TestData;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Test.TestUtils.Rainfall;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace NotionReminderService.Test.Services.BotHandlers.WeatherHandler;

public class WeatherMessageServiceTest
{
    private const string DefaultTimeStamp = "2025-10-19T20:25:00+08:00";
    
    private Mock<IWeatherApi> _weatherApi;
    private Mock<IGoogleAiApi> _googleAiApi;
    private Mock<ITelegramBotClient> _botClient;
    private Mock<IOptions<BotConfiguration>> _botConfig;
    private Mock<ILogger<IWeatherMessageService>> _logger;
    private WeatherMessageService _weatherMessageService;
    private Mock<IWeatherRepository> _weatherRepository;
    private Mock<IDateTimeProvider> _dateTimeProvider;

    [SetUp]
    public void SetUp()
    {
        _weatherApi = new Mock<IWeatherApi>();
        _googleAiApi = new Mock<IGoogleAiApi>();
        _botClient = new Mock<ITelegramBotClient>();
        _weatherRepository = new Mock<IWeatherRepository>();
        _dateTimeProvider = new Mock<IDateTimeProvider>();
        _botConfig = new Mock<IOptions<BotConfiguration>>();
        _logger = new Mock<ILogger<IWeatherMessageService>>();
        _weatherMessageService = new WeatherMessageService(_weatherApi.Object, _googleAiApi.Object, _botClient.Object,
            _weatherRepository.Object, _dateTimeProvider.Object, _botConfig.Object, _logger.Object);
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
    
    [Test]
    public async Task CreateCurrentDayRainfallIfNotExists_RainfallExists_NoCreation()
    {
        var currentDateTime = new DateTimeBuilder().WithYear(2025).WithMonth(10).WithDay(19).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(currentDateTime);
        _weatherRepository.Setup(x => x.GetRainfallByDateTime(currentDateTime.Date))
            .ReturnsAsync(new Rainfall());

        await _weatherMessageService.CreateCurrentDayRainfallIfNotExists();

        _weatherRepository.Verify(x => x.CreateRainfall(It.IsAny<DateTime>()), Times.Never);
    }

    [Test]
    public async Task CreateCurrentDayRainfallIfNotExists_RainfallNotExists_CreateNew()
    {
        var currentDateTime = new DateTimeBuilder().WithYear(2025).WithMonth(10).WithDay(19).Build();
        _dateTimeProvider.Setup(x => x.Now).Returns(currentDateTime);
        _weatherRepository.Setup(x => x.GetRainfallByDateTime(currentDateTime.Date))
            .ReturnsAsync((Rainfall)null!);

        await _weatherMessageService.CreateCurrentDayRainfallIfNotExists();

        _weatherRepository.Verify(x => x.CreateRainfall(It.IsAny<DateTime>()), Times.Once);
    }

    [Test]
    public async Task UpdateRainfallReadings_SlotMismatch_LogsWarningAndSkipsUpsert()
    {
        var systemNow = new DateTimeBuilder()
            .WithYear(2025).WithMonth(10).WithDay(19).WithHour(20).WithMinute(31)
            .Build();
        var rainfallResponse = TestDataUtils.LoadTestDataFromFile<RainfallResponse>(TestDataUtils.RainfallResponseFilePath);

        _dateTimeProvider.Setup(x => x.Now).Returns(systemNow);
        _weatherRepository.Setup(x => x.GetRainfallByDateTime(systemNow.Date))
            .ReturnsAsync(new Rainfall { RainfallId = "rainfall-23904dfs89d-sdf879sd8f9sd", Date = systemNow.Date });
        _weatherApi.Setup(x => x.GetRealTimeRainfallByLocation())
            .ReturnsAsync(rainfallResponse);
        
        await _weatherMessageService.UpdateRainfallReadings();

        _weatherRepository.Verify(x => x.UpsertRainfallSlots(It.IsAny<List<RainfallSlot>>()), Times.Never);
        _weatherRepository.Verify(x => x.RemoveRainfallSlots(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task UpdateRainfallReadings_NoCurrentDay_CreatesRainfallAndUpserts()
    {
        var systemNow = new DateTimeBuilder()
            .WithYear(2025).WithMonth(10).WithDay(19).WithHour(20).WithMinute(29)
            .Build();
        var rainfallResponse = TestDataUtils.LoadTestDataFromFile<RainfallResponse>(TestDataUtils.RainfallResponseFilePath);
        
        _dateTimeProvider.Setup(x => x.Now).Returns(systemNow);
        _weatherApi.Setup(x => x.GetRealTimeRainfallByLocation())
            .ReturnsAsync(rainfallResponse);
        _weatherRepository.Setup(x => x.GetRainfallByDateTime(systemNow.Date))
            .ReturnsAsync((Rainfall)null!);
        
        
        await _weatherMessageService.UpdateRainfallReadings();
        
        _weatherRepository.Verify(x => x.CreateRainfall(systemNow.Date), Times.Once);
        _weatherRepository.Verify(x => x.UpsertRainfallSlots(It.IsAny<List<RainfallSlot>>()), Times.Once);
    }

    [Test]
    public async Task UpdateRainfallReadings_AggregatesExistingSlot_PreservesSlotsIdAndSumsAmount()
    {
        var systemNow = new DateTimeBuilder()
            .WithYear(2025).WithMonth(10).WithDay(19).WithHour(20).WithMinute(29)
            .Build();
        var rainfallId = "rainfall-23904dfs89d-sdf879sd8f9sd";
        var rainfallResponse = TestDataUtils.LoadTestDataFromFile<RainfallResponse>(TestDataUtils.RainfallResponseFilePath);
        var rainfallSlots = new RainfallSlotBuilder()
            .WithRainfallId(rainfallId).WithHourOfDay(systemNow.Hour).WithLastTimeStamp(DefaultTimeStamp)
            .Build();
        
        _dateTimeProvider.Setup(x => x.Now).Returns(systemNow);
        _weatherApi.Setup(x => x.GetRealTimeRainfallByLocation())
            .ReturnsAsync(rainfallResponse);
        _weatherRepository.Setup(x => x.GetRainfallByDateTime(systemNow.Date))
            .ReturnsAsync(new Rainfall { RainfallId = rainfallId, Date = systemNow.Date });
        _weatherRepository.Setup(x =>
                x.GetRainFallSlots(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(rainfallSlots);
        
        await _weatherMessageService.UpdateRainfallReadings();
        
        _weatherRepository.Verify(x => 
            x.UpsertRainfallSlots(It.Is<List<RainfallSlot>>(
            slots =>
                    slots.All(slot =>
                    slot.RainfallId == rainfallId &&
                    slot.HourOfDay == systemNow.Hour)
                    &&
                    slots.Sum(rs => rs.RainfallAmount) > 0
                    )), Times.Once);
    }
}