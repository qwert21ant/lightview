using Lightview.Shared.Contracts.Settings;

namespace Lightview.Shared.Contracts.Events;

/// <summary>
/// Event published when camera monitoring settings are updated in core.
/// </summary>
public class CameraMonitoringSettingsUpdatedEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public CameraMonitoringSettings Settings { get; set; } = new();
}