using CameraControllerConnector;
using CameraManager;
using Persistence;
using WebService.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on multiple ports from configuration
var cameraApiUrl = builder.Configuration.GetSection("Kestrel:Endpoints:CameraControllerApi:Url").Value;
var cameraApiPort = ExtractPortFromUrl(cameraApiUrl) ?? 5001;
builder.WebHost.UseConfiguration(builder.Configuration.GetSection("Kestrel"));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCameraControllerClient(builder.Configuration);
builder.Services.AddCameraManager();

var app = builder.Build();

// Initialize database
await InitializeDatabaseAsync(app.Services);

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map camera controller endpoints only on the camera controller port
app.MapWhen(context => context.Connection.LocalPort == cameraApiPort, 
    cameraApp =>
    {
        cameraApp.UseRouting();
        cameraApp.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "camera-controller",
                pattern: "api/cameracontroller/{action}",
                defaults: new { controller = "CameraController" });
        });
    });

// Map main API endpoints (excluding camera controller)
app.MapWhen(context => context.Connection.LocalPort != cameraApiPort,
    mainApp =>
    {
        mainApp.UseRouting();
        mainApp.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<CameraHub>("/cameraHub");
        });
    });

app.Run();

static int? ExtractPortFromUrl(string? url)
{
    if (string.IsNullOrEmpty(url)) return null;
    var uri = new Uri(url.Replace("*", "localhost"));
    return uri.Port;
}

static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Initializing database...");
        
        // Ensure database is created and apply any pending migrations
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to initialize database. Application will terminate.");
        throw;
    }
}