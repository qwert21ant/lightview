using WebService.Services;
using WebService.Factories;
using WebService.Configuration;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;

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

// Register HTTP client factory
builder.Services.AddHttpClient();

// Register HTTP client for MediaMTX
builder.Services.AddHttpClient<IMediaMtxService, MediaMtxService>();

// Register factories
builder.Services.AddSingleton<ICameraFactory, CameraFactory>();
builder.Services.AddSingleton<ICameraMonitoringFactory, CameraMonitoringFactory>();

// Register application services
builder.Services.AddSingleton<ICameraService, CameraService>();
builder.Services.AddSingleton<IEventPublisherService, EventPublisherService>();
builder.Services.AddSingleton<IMediaMtxService, MediaMtxService>();

// Register background services
builder.Services.AddHostedService<CoreSyncService>();

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

app.Run();
    