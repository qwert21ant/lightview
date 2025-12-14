using Lightview.Shared.Contracts.Settings;

namespace CameraController.Contracts.Interfaces;

public interface ISettingsService
{
    CameraMonitoringSettings CameraMonitoringSettings { get; set; }
}