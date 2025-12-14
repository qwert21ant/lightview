using Microsoft.AspNetCore.SignalR;
using Lightview.Shared.Contracts.Settings;
using SettingsManager.Interfaces;
using WebService.Services.Events;

namespace WebService.Hubs;

public class SettingsHub : Hub
{
    private readonly ISettingsService _settingsService;
    private readonly SettingsEventPublisher _settingsEventPublisher;
    private readonly ILogger<SettingsHub> _logger;

    public SettingsHub(ISettingsService settingsService, SettingsEventPublisher settingsEventPublisher, ILogger<SettingsHub> logger)
    {
        _settingsService = settingsService;
        _settingsEventPublisher = settingsEventPublisher;
        _logger = logger;
    }

    // Hub methods
    public async Task<CameraMonitoringSettings> GetCameraMonitoringSettings()
    {
        var settings = await _settingsService.GetCameraMonitoringSettingsAsync();
        return settings;
    }

    public async Task UpdateCameraMonitoringSettings(CameraMonitoringSettings settings)
    {
        await _settingsService.UpdateCameraMonitoringSettingsAsync(settings);
        await _settingsEventPublisher.PublishCameraMonitoringSettingsUpdatedAsync(settings);
        await Clients.All.SendAsync("SettingsUpdated", settings);
        _logger.LogInformation("SettingsHub: CameraMonitoringSettings updated and broadcasted");
    }
}
