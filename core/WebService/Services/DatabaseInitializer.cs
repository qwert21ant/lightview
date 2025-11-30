using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Models;

namespace WebService.Services;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        AppDbContext context, 
        IPasswordService passwordService, 
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Apply any pending migrations
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully");

            // Create admin user if it doesn't exist
            await CreateAdminUserAsync();
            
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    private async Task CreateAdminUserAsync()
    {
        var adminUsername = _configuration["Admin:Username"] ?? "admin";
        var adminPassword = _configuration["Admin:Password"];

        if (string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogWarning("Admin password not configured in environment variables. Skipping admin user creation.");
            return;
        }

        var existingAdmin = await _context.Users
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
            PasswordHash = _passwordService.HashPassword(adminPassword),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin user created successfully with username: {Username}", adminUsername);
    }
}