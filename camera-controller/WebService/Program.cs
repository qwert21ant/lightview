using WebService.Services;
using WebService.Factories;
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

// Register HTTP client for MediaMTX
builder.Services.AddHttpClient<IMediaMtxService, MediaMtxService>();

// Register factories
builder.Services.AddSingleton<ICameraFactory, CameraFactory>();
builder.Services.AddSingleton<ICameraMonitoringFactory, CameraMonitoringFactory>();

// Register application services
builder.Services.AddSingleton<ICameraService, CameraService>();
builder.Services.AddSingleton<IEventPublisherService, EventPublisherService>();
builder.Services.AddSingleton<IMediaMtxService, MediaMtxService>();

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
    