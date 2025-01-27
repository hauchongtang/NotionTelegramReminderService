namespace NotionReminderService.Test.Services.BotHandlers.UpdateHandler;

public class UpdateServiceTest 
{
	[Setup]
	public void Setup()
	{
		_botClient = new Mock<ITelegramBotClient>();
		_weatherMsgService = new Mock<IWeatherMessageService>();
		_notionService = new Mock<INotionService>();
		_googleAiApi = new Mock<IGoogleAiApi>();
		_dtProvider = new Mock<IDateTimeProvider>();
		_notionConfig = new Mock<IOptions<NotionConfig>();
		_logger = new Mock<ILogger<IUpdateService>>();
		
		_updateService = new UpdateService(_botClient.Object, _weatherMsgService.Object, _notionService.Object
			_googleAiApi.Object, _dtProvider.Object, _notionConfig.Object, _logger.Object);
	}
	
	[Test]
	public async Task OnMessage_NoBotHint_DidNotSendMessageResponse()
	{
		var messageReceived = new Message 
		{
			Text = "hi tom! I have an event today!"
		}
		
		await _updateService.OnMessage(messageReceived);
		
		_botClient.Verify(x => 
			x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
	}
	
	[Test]
	public async Task OnMessage_BotHintReceivedMessageUnparsable_DidNotSendMessageResponse()
	{
		var messageReceived = new Message 
		{
			Text = "hi bot, I have an event today!"
		}
		
		await _updateService.OnMessage(messageReceived);
		
		_botClient.Verify(x => 
		x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
	}
	
	[Test]
	public async Task OnMessage_BotHintReceivedNotionServiceNotWorking_DidNotSendMessageResponse()
	{
		var messageReceived = new Message
		{
			Text = "hi bot, please create an event at xxxx tomorrow at yyyy"
		}
		
		await _updateService.OnMessage(messageReceived);
		
		// TODO: Add another verification that correct error message is sent to the chat
		_botClient.Verify(x => 
			x.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<Message>(), Times.Once));
	}
	
	[Test]
	public async Task OnMessage_BotHintReceivedAllParsable_SendMessageResponse()
	{
		var messageReceived = new Message
		{
			Text = "hi bot, please create an event located at xxxx tomorrow at noon."
		}
		
		var user1 = new User
		{
			Name = "user1"
		}
		var peopleProperty = new PeoplePropertyBuilder().WithUser(user1).Build();
		var paginatedList = new PaginatedListBuilder()
			.AddNewPage();
		_notionService.Setup(x => x.GetPaginatedList(It.IsAny<DatabaseQueryParamters>)).ReturnsAsync();
	}
}