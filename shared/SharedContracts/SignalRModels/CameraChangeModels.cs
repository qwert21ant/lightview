using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.Events;

namespace Lightview.Shared.Contracts.SignalRModels;

/// <summary>
/// Base model for all camera change notifications sent via SignalR
/// </summary>
public class CameraChangedNotification
{
    public Guid CameraId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Data models for different types of camera change events
/// </summary>
public class CameraStatusChangedData
{
    public CameraStatus PreviousStatus { get; set; }
    public CameraStatus CurrentStatus { get; set; }
    public string? Reason { get; set; }
}

public class CameraErrorData
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
    public bool IsRecoverable { get; set; }
}

public class PtzMovedData
{
    public PtzPosition? PreviousPosition { get; set; }
    public PtzPosition? CurrentPosition { get; set; }
    public PtzMoveType MoveType { get; set; }
}

public class CameraStatisticsData
{
    public TimeSpan Uptime { get; set; }
    public long BytesReceived { get; set; }
    public double AverageFps { get; set; }
    public int DroppedFrames { get; set; }
    public TimeSpan AverageLatency { get; set; }
}

public class CameraMetadataUpdatedData
{
    public List<CameraProfile>? Profiles { get; set; }
    public CameraCapabilities? Capabilities { get; set; }
    public CameraDeviceInfo? DeviceInfo { get; set; }
    public string UpdateType { get; set; } = string.Empty;
}

/// <summary>
/// Constants for camera event types
/// </summary>
public static class CameraEventTypes
{
    public const string StatusChanged = "StatusChanged";
    public const string Error = "Error";
    public const string PtzMoved = "PtzMoved";
    public const string Statistics = "Statistics";
    public const string MetadataUpdated = "MetadataUpdated";
}