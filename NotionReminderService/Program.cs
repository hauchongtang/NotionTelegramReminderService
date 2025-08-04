using NotionReminderService.HostedServices.TelegramBot;
using NotionReminderService.Utils;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .Enrich.FromLogContext());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SecretKeyHeader>();
});

// Setup configuration
builder.Services.ConfigureTelegramBotClient(builder.Configuration);

builder.Services.AddConfigurations(builder.Configuration);

// Notion Configuration
builder.Services.ConfigureNotionClient(builder.Configuration); 

// APIs
builder.Services.ConfigureApis();

// Databases
builder.Services.ConfigureDatabase(builder.Configuration);

// Repositories
builder.Services.ConfigureRepositories();

// Services
builder.Services.ConfigureServices();

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
app.UseSwagger();
app.UseSwaggerUI();

app.UseHealthChecks("/health");

app.Run();