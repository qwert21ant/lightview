using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using Lightview.Shared.Contracts.Settings;

namespace CoreConnector;

public interface ICoreConnector
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<List<CameraInitializationResponse>?> GetCamerasAsync(CancellationToken cancellationToken = default);
    Task<CameraMonitoringSettings?> GetCameraMonitoringSettingsAsync(CancellationToken cancellationToken = default);
}
