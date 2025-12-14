using WebService.Services;
using WebService.Factories;
using WebService.Configuration;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using RtspCamera.Services;

using WebService.Services.Events;
using RabbitMQShared.Extensions;
using CoreConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Camera Controller API",
        Version = "v1",
        Description = "API for managing IP cameras and streaming through MediaMTX"
    });
});

// Configure MediaMTX
var mediaMtxConfig = builder.Configuration.GetSection("MediaMtx").Get<MediaMtxConfiguration>() 
    ?? throw new InvalidOperationException("MediaMtx configuration is required");
builder.Services.AddSingleton(mediaMtxConfig);

// Configure Core Service connection
var coreServiceConfig = builder.Configuration.GetSection("CoreService").Get<CoreServiceConfiguration>()
    ?? throw new InvalidOperationException("CoreService configuration is required");
builder.Services.AddSingleton(coreServiceConfig);
builder.Services.Configure<CoreServiceConfiguration>(builder.Configuration.GetSection("CoreService"));

// Add configuration services
builder.Services.AddSingleton<ISettingsService, SettingsCacheService>();

// Configure RabbitMQ using shared infrastructure
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddRabbitMQPublisher();
builder.Services.AddRabbitMQConsumer<SettingsEventConsumer>();
builder.Services.AddSingleton<CameraEventPublisher>();

// Register HTTP client factory
builder.Services.AddHttpClient();

// Register typed HttpClient for CoreConnector
builder.Services.AddHttpClient<ICoreConnector, CoreConnectorService>(client =>
{
    client.BaseAddress = new Uri(coreServiceConfig.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(coreServiceConfig.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-API-Key", coreServiceConfig.ApiKey);
});

// Register HTTP client for MediaMTX
builder.Services.AddHttpClient<IMediaMtxService, MediaMtxService>();

// Register factories
builder.Services.AddSingleton<ICameraFactory, CameraFactory>();
builder.Services.AddSingleton<ICameraMonitoringFactory, CameraMonitoringFactory>();

// Register application services
builder.Services.AddSingleton<ICameraService, CameraService>();
builder.Services.AddSingleton<IMediaMtxService, MediaMtxService>();

// Register RtspCamera services
builder.Services.AddSingleton<FfmpegSnapshotService>();

// Register initializable services
builder.Services.AddInitializableSingleton<CoreSyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Camera Controller API v1");
    });
}

app.MapControllers();

// Initialize mandatory services before running the app
await app.Services.InitializeMandatoryServicesAsync();

// Wire settings event consumer to update cache
var settingsConsumer = app.Services.GetRequiredService<SettingsEventConsumer>();
var settingsService = app.Services.GetRequiredService<ISettingsService>();
settingsConsumer.CameraMonitoringSettingsUpdated += async evt =>
{
    settingsService.CameraMonitoringSettings = evt.Settings;
    await Task.CompletedTask;
};

app.Run();
    