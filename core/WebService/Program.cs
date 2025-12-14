using CameraControllerConnector;
using CameraManager;
using Persistence;
using WebService.Hubs;
using WebService.Services;
using WebService.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using SettingsManager.Interfaces;
using SettingsManager.Services;
using RabbitMQShared.Extensions;
using WebService.Services.Initialization;
using WebService.Services.Auth;
using WebService.Services.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on multiple ports
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Main App
    options.ListenAnyIP(5001); // CameraController
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCameraControllerClient(builder.Configuration);
builder.Services.AddCameraManager();

// Configure RabbitMQ using shared infrastructure
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddRabbitMQConsumer<CameraEventConsumer>();
builder.Services.AddRabbitMQPublisher();
builder.Services.AddSingleton<SettingsEventPublisher>();

// Add camera event handling
builder.Services.AddSingleton<CameraEventHandlerService>();
builder.Services.AddSingleton<SettingsEventPublisher>();

// Add authentication services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddMemoryCache();

// Add initialization services
builder.Services.AddInitializableSingleton<DatabaseInitialization>();
builder.Services.AddInitializableSingleton<SettingsInitializationService>();

// Add JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-very-secure-secret-key-that-is-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "lightview";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "lightview-client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        
        // Allow JWT tokens in SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/cameraHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationSchemeOptions.DefaultScheme, options => { });

builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("public", new () { Title = "Public API", Version = "v1" });
    c.SwaggerDoc("internal", new () { Title = "Internal API", Version = "v1" });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "public")
        {
            return apiDesc.GroupName == "public";
        }
        else if (docName == "internal")
        {
            return apiDesc.GroupName == "internal";
        }
        return false;
    });
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow Vite dev server (typically on port 5173)
            policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:3000"
            );
        }
        else
        {
            // Production: Allow specific origins
            policy.WithOrigins(
                "http://localhost",
                "http://localhost:80",
                "http://localhost:3000",
                "https://localhost:3000",
                "http://frontend", // Docker container name
                "http://frontend:80"
            );
        }
        
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Initialize all mandatory services before starting the app
await app.Services.InitializeMandatoryServicesAsync();

// Initialize camera event forwarding service
InitializeCameraEventHandlerService(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/public/swagger.json", "Public API");
        c.SwaggerEndpoint("/swagger/internal/swagger.json", "Internal API");
    });
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CameraHub>("/cameraHub").RequireCors("AllowFrontend");
app.MapHub<SettingsHub>("/settingsHub").RequireCors("AllowFrontend");

app.Run();

static void InitializeCameraEventHandlerService(IServiceProvider serviceProvider)
{
    // Initialize the event handler service to ensure event subscriptions are set up
    var eventHandlerService = serviceProvider.GetRequiredService<CameraEventHandlerService>();
    Console.WriteLine("Camera event handler service initialized");
}
