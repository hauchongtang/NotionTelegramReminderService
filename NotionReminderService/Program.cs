using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.HostedServices.TelegramBot;
using NotionReminderService.Services.BotHandlers.MessageHandler;
using NotionReminderService.Services.BotHandlers.UpdateHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Services.NotionHandlers;
using NotionReminderService.Services.NotionHandlers.NotionEventParser;
using NotionReminderService.Services.NotionHandlers.NotionEventUpdater;
using NotionReminderService.Services.NotionHandlers.NotionService;
using NotionReminderService.Utils;
using Serilog;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Setup configuration
var botConfigSection = builder.Configuration.GetSection("BotConfiguration");
builder.Services.Configure<BotConfiguration>(botConfigSection);
builder.Services.AddHttpClient("webhook").AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));

var weatherConfigSection = builder.Configuration.GetSection("WeatherConfiguration");
builder.Services.Configure<WeatherConfiguration>(weatherConfigSection);

var googleAiConfigSection = builder.Configuration.GetSection("GoogleAiConfiguration");
builder.Services.Configure<GoogleAiConfiguration>(googleAiConfigSection);

// Notion Configuration
var notionConfiguration = builder.Configuration.GetSection("NotionConfiguration");
builder.Services.Configure<NotionConfiguration>(notionConfiguration);
builder.Services.AddNotionClient(options =>
{
    options.AuthToken = notionConfiguration.Get<NotionConfiguration>()!.NotionAuthToken;
});

// APIs
builder.Services.AddScoped<IWeatherApi, WeatherApi>();
builder.Services.AddScoped<IGoogleAiApi, GoogleAiApi>();

// Services
builder.Services.AddScoped<IUpdateService, UpdateService>();
builder.Services.AddScoped<INotionEventParserService, NotionEventParserService>();
builder.Services.AddScoped<IEventsMessageService, EventsMessageService>();
builder.Services.AddScoped<INotionService, NotionService>();
builder.Services.AddScoped<IWeatherMessageService, WeatherMessageService>();
builder.Services.AddScoped<INotionEventUpdaterService, NotionEventUpdaterService>();
builder.Services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();

// Hosted Services
builder.Services.AddHostedService<TelegramBotSetup>();

// Attributes
builder.Services.AddScoped<SecretKeyValidationAttribute>();

builder.Services.AddAuthentication();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.ConfigureTelegramBotMvc();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHealthChecks("/health");

app.Run();