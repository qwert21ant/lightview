using CameraController.Contracts.Interfaces;
using Lightview.Shared.Contracts.Settings;

namespace WebService.Services;

/// <summary>
/// Singleton service to cache configuration retrieved from core service
/// </summary>
public class SettingsCacheService : ISettingsService
{
    private CameraMonitoringSettings _cameraMonitoringSettings = new();
    private readonly ILogger<SettingsCacheService> _logger;

    public SettingsCacheService(ILogger<SettingsCacheService> logger)
    {
        _logger = logger;
    }

    public CameraMonitoringSettings CameraMonitoringSettings
    {
        get
        {
            return _cameraMonitoringSettings;
        }

        set
        {
            _cameraMonitoringSettings = value;
            _logger.LogDebug("Camera monitoring settings updated in cache");
        }
    }
}