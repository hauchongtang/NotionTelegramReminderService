using NotionReminderService.Config;
using NotionReminderService.Services.BotHandlers.MessageService;
using NotionReminderService.Services.BotHandlers.UpdateService;
using NotionReminderService.Services.NotionHandlers;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;
using Serilog;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Setup bot configuration
var botConfigSection = builder.Configuration.GetSection("BotConfiguration");
builder.Services.Configure<BotConfiguration>(botConfigSection);
builder.Services.AddHttpClient("webhook").AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddScoped<INotionEventParserService, NotionEventParserService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotionService, NotionService>();
builder.Services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();

builder.Services.AddAuthentication();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.ConfigureTelegramBotMvc();

// Notion Configuration
var notionConfiguration = builder.Configuration.GetSection("NotionConfiguration");
builder.Services.Configure<NotionConfiguration>(notionConfiguration);
builder.Services.AddNotionClient(options =>
{
    options.AuthToken = notionConfiguration.Get<NotionConfiguration>()!.NotionAuthToken;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHealthChecks("/health");

app.Run();