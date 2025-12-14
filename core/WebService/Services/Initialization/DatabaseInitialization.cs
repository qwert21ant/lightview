using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Models;
using RabbitMQShared.Interfaces;
using WebService.Services.Auth;

namespace WebService.Services.Initialization;

public class DatabaseInitialization : IInitializable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitialization> _logger;

    public string ServiceName => "Database Initialization";

    public DatabaseInitialization(
        IServiceProvider serviceProvider, 
        IConfiguration configuration,
        ILogger<DatabaseInitialization> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Database created/verified successfully");

            // Create admin user if it doesn't exist
            await CreateAdminUserAsync(context, passwordService);
            
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    private async Task CreateAdminUserAsync(AppDbContext context, IPasswordService passwordService)
    {
        var adminUsername = _configuration["Admin:Username"] ?? "admin";
        var adminPassword = _configuration["Admin:Password"];

        if (string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogWarning("Admin password not configured in environment variables. Skipping admin user creation.");
            return;
        }

        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Username == adminUsername);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists");
            return;
        }

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = adminUsername,
            PasswordHash = passwordService.HashPassword(adminPassword),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        _logger.LogInformation("Admin user created successfully with username: {Username}", adminUsername);
    }
}