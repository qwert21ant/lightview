using SettingsManager.Interfaces;
using Lightview.Shared.Contracts.Settings;
using RabbitMQShared.Interfaces;

namespace WebService.Services.Initialization;

/// <summary>
/// Service that initializes default system settings on application startup
/// </summary>
public class SettingsInitializationService : IInitializable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsInitializationService> _logger;

    public string ServiceName => "Settings Initialization Service";

    public SettingsInitializationService(
        IServiceProvider serviceProvider,
        ILogger<SettingsInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing default system settings...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

            // Check if camera monitoring defaults already exist
            try {
                var existingSettings = await settingsService.GetCameraMonitoringSettingsAsync();

                _logger.LogInformation("Camera monitoring defaults already exist");
            }
            catch (Exception)
            {
                var defaultSettings = new CameraMonitoringSettings();

                await settingsService.UpdateCameraMonitoringSettingsAsync(defaultSettings);
                
                _logger.LogInformation("Default camera monitoring settings created");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize default system settings");
            throw;
        }
    }
}