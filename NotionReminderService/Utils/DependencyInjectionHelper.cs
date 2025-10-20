using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.PlanPulseAgent;
using NotionReminderService.Api.Transport;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.Models;
using NotionReminderService.Repositories.Transport;
using NotionReminderService.Repositories.Weather;
using NotionReminderService.Services.BotHandlers.MessageHandler;
using NotionReminderService.Services.BotHandlers.TransportHandler;
using NotionReminderService.Services.BotHandlers.UpdateHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Services.NotionHandlers.NotionEventRetrival;
using NotionReminderService.Services.NotionHandlers.NotionEventUpdater;
using NotionReminderService.Services.NotionHandlers.NotionService;
using Telegram.Bot;

namespace NotionReminderService.Utils;

public static class DependencyInjectionHelper
{
    public static IServiceCollection ConfigureNotionClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var notionConfiguration = configuration.GetSection("NotionConfiguration");
        services.Configure<NotionConfiguration>(notionConfiguration);
        services.AddNotionClient(options =>
        {
            options.AuthToken = notionConfiguration.Get<NotionConfiguration>()!.NotionAuthToken;
        });
        return services;
    }

    public static IServiceCollection ConfigureTelegramBotClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var botConfigSection = configuration.GetSection("BotConfiguration");
        services.Configure<BotConfiguration>(botConfigSection);
        services.AddHttpClient("webhook").AddTypedClient<ITelegramBotClient>(
            httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));
        return services;
    }

    public static IServiceCollection ConfigureApis(this IServiceCollection services)
    {
        services.AddScoped<IWeatherApi, WeatherApi>();
        services.AddScoped<IGoogleAiApi, GoogleAiApi>();
        services.AddScoped<IPlanPulseAgent, PlanPulseAgent>();
        services.AddScoped<ITransportApi, TransportApi>();
        return services;
    }

    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddNpgsql<DatabaseContext>(connectionString);
        return services;
    }

    public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITransportRepository, TransportRepository>();
        services.AddScoped<IWeatherRepository, WeatherRepository>();
        return services;
    }

    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<IUpdateService, UpdateService>();
        services.AddScoped<INotionEventRetrivalService, NotionEventRetrivalService>();
        services.AddScoped<IEventsMessageService, EventsMessageService>();
        services.AddScoped<INotionService, NotionService>();
        services.AddScoped<IWeatherMessageService, WeatherMessageService>();
        services.AddScoped<ITransportService, TransportService>();
        services.AddScoped<INotionEventUpdaterService, NotionEventUpdaterService>();
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }

    public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        var weatherConfigSection = configuration.GetSection("WeatherConfiguration");
        services.Configure<WeatherConfiguration>(weatherConfigSection);

        var googleApiConfigSection = configuration.GetSection("GoogleApiConfiguration");
        services.Configure<GoogleApiConfiguration>(googleApiConfigSection);

        var googleAiConfigSection = configuration.GetSection("GoogleAiConfiguration");
        services.Configure<GoogleAiConfiguration>(googleAiConfigSection);

        var planPulseAgentConfigSection = configuration.GetSection("PlanPulseAgentConfiguration");
        services.Configure<PlanPulseAgentConfiguration>(planPulseAgentConfigSection);

        var transportConfigSection = configuration.GetSection("TransportConfiguration");
        services.Configure<TransportConfiguration>(transportConfigSection);
        
        var browserConfigSection = configuration.GetSection("BrowserConfiguration");
        services.Configure<BrowserConfiguration>(browserConfigSection);
        return services;
    }
}