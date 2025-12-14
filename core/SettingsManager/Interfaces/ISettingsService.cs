using Lightview.Shared.Contracts.Settings;

namespace SettingsManager.Interfaces;

public interface ISettingsService
{
    Task<T?> GetSettingAsync<T>(string name) where T : class;
    Task SetSettingAsync<T>(string name, T value) where T : class;
    Task<CameraMonitoringSettings> GetCameraMonitoringSettingsAsync();
    Task UpdateCameraMonitoringSettingsAsync(CameraMonitoringSettings settings);
}