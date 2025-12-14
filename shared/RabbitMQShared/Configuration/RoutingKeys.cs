namespace RabbitMQShared.Configuration;

/// <summary>
/// Message broker routing keys for different event types
/// </summary>
public static class CameraEventRoutingKeys
{
    public const string StatusChanged = "camera.status.changed";
    public const string Error = "camera.error";
    public const string PtzMoved = "camera.ptz.moved";
    public const string CameraStatistics = "camera.statistics";
    public const string MetadataUpdated = "camera.metadata.updated";
    public const string SnapshotCaptured = "camera.snapshot.captured";
}

/// <summary>
/// Message broker routing keys for settings-related events
/// </summary>
public static class SettingsEventRoutingKeys
{
    public const string CameraMonitoringUpdated = "settings.camera-monitoring.updated";
}