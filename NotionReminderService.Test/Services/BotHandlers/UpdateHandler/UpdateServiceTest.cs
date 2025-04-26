using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Notion.Client;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Config;
using NotionReminderService.Models.GoogleAI;
using NotionReminderService.Models.NotionEvent;
using NotionReminderService.Services.BotHandlers.TransportHandler;
using NotionReminderService.Services.BotHandlers.UpdateHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Test.TestUtils.Page;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using User = Notion.Client.User;

namespace NotionReminderService.Test.Services.BotHandlers.UpdateHandler;

public class UpdateServiceTest 
{
	private Mock<ITelegramBotClient> _botClient;
	private Mock<IWeatherMessageService> _weatherMsgService;
	private Mock<INotionService> _notionService;
	private Mock<IGoogleAiApi> _googleAiApi;
	private Mock<IDateTimeProvider> _dtProvider;
	private Mock<IOptions<NotionConfiguration>> _notionConfig;
	private Mock<ILogger<IUpdateService>> _logger;
	private Page _pageWithProperties;
	private UpdateService _updateService;
	private NotionEvent? _notionEvent;
	private Mock<ITransportService> _transportService;

	[SetUp]
	public void Setup()
	{
		_botClient = new Mock<ITelegramBotClient>();
		_weatherMsgService = new Mock<IWeatherMessageService>();
		_notionService = new Mock<INotionService>();
		_googleAiApi = new Mock<IGoogleAiApi>();
		_transportService = new Mock<ITransportService>();
		_dtProvider = new Mock<IDateTimeProvider>();
		_notionConfig = new Mock<IOptions<NotionConfiguration>>();
		var notionConfig = new NotionConfiguration
		{
			NotionAuthToken = "123",
			DatabaseId = "123"
		};
		_notionConfig.Setup(x => x.Value).Returns(notionConfig);
		_logger = new Mock<ILogger<IUpdateService>>();
		
		var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build();
		_pageWithProperties = new NotionPageBuilder()
			.WithProperty("Name", new TitlePropertyBuilder().WithTitle("Event 1").Build())
			.WithProperty("Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build())
			.WithProperty("Where", new RichTextPropertyBuilder().WithText("Singapore").Build())
			.WithProperty("Person", new PeoplePropertyBuilder()
				.WithUser(new User { Name = "User1" })
				.WithUser(new User { Name = "User2" })
				.Build())
			.WithProperty("Status", new StatusPropertyBuilder().WithStatus("testStatus").Build())
			.WithProperty("Mini Reminder Description", new RichTextPropertyBuilder()
				.WithText("Mini reminder desc")
				.Build())
			.WithProperty("Trigger Mini Reminder", new SelectPropertyBuilder()
				.WithSelectOption(new SelectOption{ Name = "On the day itself"})
				.Build())
			.Build();
		_notionEvent = NotionEventParser.GetNotionEvent(_pageWithProperties);
		
		_updateService = new UpdateService(_botClient.Object, _weatherMsgService.Object, _notionService.Object,
			_googleAiApi.Object, _transportService.Object, _dtProvider.Object, _notionConfig.Object, _logger.Object);
	}
	
	[Test]
	public async Task OnMessage_NoBotHint_DidNotSendMessageResponse()
	{
		var messageReceived = new Message 
		{
			Text = "hi tom! I have an event today!"
		};

		var updateObj = new Update
		{
			Message = messageReceived
		};
		await _updateService.HandleUpdateAsync(updateObj, new CancellationToken());
		
		_botClient.Verify(x => 
			x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
	}
	
	[Test]
	public async Task OnMessage_MissingNotionTagInfoPage_SendsErrorMessage()
	{
		_notionService.Setup(x =>
			x.GetPageFromDatabase(It.IsAny<DatabasesQueryParameters>()));
		var messageReceived = new Message 
		{
			Text = "hi bot, I have an event today!"
		};
		var updateObj = new Update
		{
			Message = messageReceived
		};
		await _updateService.HandleUpdateAsync(updateObj, new CancellationToken());
		
		_botClient.Verify(x => 
		x.SendRequest(It.Is<IRequest<Message>>(y => 
			((SendMessageRequest)y).Text.Equals("We are unable to generate your event. Please try again later.")), 
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task OnMessage_NotionClientApiDown_SendsErrorMessage()
	{
		_notionService.Setup(x => x.GetPageFromDatabase(It.IsAny<DatabasesQueryParameters>()))
			.ThrowsAsync(new Exception("Client error ..."));
		
		var messageReceived = new Message 
		{
			Text = "hi bot, I have an event today!"
		};
		var updateObj = new Update
		{
			Message = messageReceived
		};
		await _updateService.HandleUpdateAsync(updateObj, new CancellationToken());
		
		_botClient.Verify(x => 
			x.SendRequest(It.Is<IRequest<Message>>(y => 
					((SendMessageRequest)y).Text.Equals("The Notion Connector seems to be down right now! Please try again later.")),
				It.IsAny<CancellationToken>()), Times.Once);
	}
	
	[Test]
	public async Task OnMessage_SuccessFlow_SendMessageResponse()
	{
		var user1 = new User { Name = "User1" };
		var user2 = new User { Name = "User2" };
		var tagInfoPage = new NotionPageBuilder()
			.WithProperty("Person", new PeoplePropertyBuilder()
				.WithUser(user1)
				.WithUser(user2)
				.Build())
			.WithProperty("PersonOneMap", new RichTextPropertyBuilder().WithText("123456-User1").Build())
			.WithProperty("PersonTwoMap", new RichTextPropertyBuilder().WithText("654321-User2").Build())
			.WithProperty("Tags", new MultiSelectPropertyBuilder()
				.WithSelectOption(new SelectOption{ Name = "Option1" })
				.WithSelectOption(new SelectOption{ Name = "Option2" })
				.Build())
			.Build();
		_notionService.Setup(x =>
			x.GetPageFromDatabase(It.IsAny<DatabasesQueryParameters>())).ReturnsAsync(tagInfoPage);

		var notionNewEvent = new NotionNewEvent
		{
			Name = _notionEvent?.Name,
			Where = _notionEvent?.Where,
			Person = "123456-User1~654321-User2",
			Start = _notionEvent?.Start,
			End = _notionEvent?.End
		};
		var jsonText = JsonConvert.SerializeObject(notionNewEvent);
		var geminiResponse = new GeminiMessageResponse
		{
			Candidates = [new() { Content = new() { Parts = [new() { Text = jsonText }] } }]
		};
		_googleAiApi.Setup(x => x.GenerateContent(It.IsAny<string>())).ReturnsAsync(geminiResponse);

		_notionService.Setup(x => 
			x.CreateNewEvent(It.IsAny<PagesCreateParameters>())).ReturnsAsync(_pageWithProperties);

		var messageReceived = new Message
		{
			From = new Telegram.Bot.Types.User
			{
				Id = 123456
			},
			Chat = new Chat
			{
				Id = 98765
			},
			Text = "hi bot, please create an event located at xxxx tomorrow at noon."
		};
		var updateObj = new Update
		{
			Message = messageReceived
		};
		await _updateService.HandleUpdateAsync(updateObj, new CancellationToken());
		
		_botClient.Verify(x => 
			x.SendRequest(It.Is<IRequest<Message>>(y => 
					((SendMessageRequest)y).Text.Contains("Event created successfully! Here are the details:")),
				It.IsAny<CancellationToken>()), Times.Once);
	}
}