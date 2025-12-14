namespace Lightview.Shared.Contracts.Events;

/// <summary>
/// Base class for all camera-related events
/// </summary>
public abstract class CameraEventBase
{
    public Guid CameraId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

/// <summary>
/// Event published when a camera's status changes
/// </summary>
public class CameraStatusChangedEvent : CameraEventBase
{
    public CameraStatus PreviousStatus { get; set; }
    public CameraStatus CurrentStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a camera encounters an error
/// </summary>
public class CameraErrorEvent : CameraEventBase
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
    public bool IsRecoverable { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Event published when PTZ movement occurs
/// </summary>
public class PtzMovedEvent : CameraEventBase
{
    public PtzPosition? PreviousPosition { get; set; }
    public PtzPosition? CurrentPosition { get; set; }
    public PtzMoveType MoveType { get; set; }
}

/// <summary>
/// Event published when camera statistics are collected
/// </summary>
public class CameraStatisticsEvent : CameraEventBase
{
    public TimeSpan Uptime { get; set; }
    public long BytesReceived { get; set; }
    public double AverageFps { get; set; }
    public int DroppedFrames { get; set; }
    public TimeSpan AverageLatency { get; set; }
}

/// <summary>
/// Event published when camera metadata (profiles, capabilities, device info) is updated
/// </summary>
public class CameraMetadataUpdatedEvent : CameraEventBase
{
    public List<CameraProfile>? Profiles { get; set; }
    public CameraCapabilities? Capabilities { get; set; }
    public CameraDeviceInfo? DeviceInfo { get; set; }
    
    /// <summary>
    /// Indicates which metadata fields were updated
    /// </summary>
    public CameraMetadataUpdateType UpdateType { get; set; }
}

/// <summary>
/// Event published when a camera snapshot is captured
/// </summary>
public class CameraSnapshotCapturedEvent : CameraEventBase
{
    /// <summary>
    /// JPEG image data as base64 string
    /// </summary>
    public string ImageData { get; set; } = string.Empty;
    
    /// <summary>
    /// Image file size in bytes
    /// </summary>
    public int ImageSize { get; set; }
    
    /// <summary>
    /// Profile token used for capture
    /// </summary>
    public string? ProfileToken { get; set; }
    
    /// <summary>
    /// Time taken to capture the snapshot
    /// </summary>
    public TimeSpan CaptureTime { get; set; }
    
    /// <summary>
    /// Resolution of the captured image
    /// </summary>
    public string? Resolution { get; set; }
}

/// <summary>
/// Flags indicating which camera metadata was updated
/// </summary>
[Flags]
public enum CameraMetadataUpdateType
{
    Profiles = 1,
    Capabilities = 2,
    DeviceInfo = 4,
    All = Profiles | Capabilities | DeviceInfo
}

/// <summary>
/// Severity levels for camera errors
/// </summary>
public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
