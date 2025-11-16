namespace Lightview.Shared.Contracts;

// RabbitMQ Event Models
public abstract class BaseEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public Guid? CameraId { get; set; }
    public string Source { get; set; } = "CameraController";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Camera Status Events
public class CameraStatusChangedEvent : BaseEvent
{
    public CameraStatus PreviousStatus { get; set; }
    public CameraStatus CurrentStatus { get; set; }
    public string? Reason { get; set; }
    public string? ErrorMessage { get; set; }

    public CameraStatusChangedEvent()
    {
        EventType = "CameraStatusChanged";
    }
}

public class CameraConnectedEvent : BaseEvent
{
    public OnvifDeviceInfo DeviceInfo { get; set; } = new();
    public CameraCapabilities Capabilities { get; set; } = new();
    public List<CameraProfile> AvailableProfiles { get; set; } = new();

    public CameraConnectedEvent()
    {
        EventType = "CameraConnected";
    }
}

public class CameraDisconnectedEvent : BaseEvent
{
    public string Reason { get; set; } = string.Empty;
    public bool IsExpected { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }

    public CameraDisconnectedEvent()
    {
        EventType = "CameraDisconnected";
    }
}

public class CameraErrorEvent : BaseEvent
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    public bool IsRecoverable { get; set; } = true;

    public CameraErrorEvent()
    {
        EventType = "CameraError";
    }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

// Stream Events
public class StreamStartedEvent : BaseEvent
{
    public string StreamPath { get; set; } = string.Empty;
    public string ProfileToken { get; set; } = string.Empty;
    public StreamConfiguration Configuration { get; set; } = new();

    public StreamStartedEvent()
    {
        EventType = "StreamStarted";
    }
}

public class StreamStoppedEvent : BaseEvent
{
    public string StreamPath { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public StreamStatistics? FinalStatistics { get; set; }

    public StreamStoppedEvent()
    {
        EventType = "StreamStopped";
    }
}

public class StreamHealthEvent : BaseEvent
{
    public string StreamPath { get; set; } = string.Empty;
    public StreamStatus Status { get; set; } = new();
    public List<string> HealthIssues { get; set; } = new();
    public bool IsHealthy { get; set; } = true;

    public StreamHealthEvent()
    {
        EventType = "StreamHealth";
    }
}

// PTZ Events
public class PtzMovedEvent : BaseEvent
{
    public PtzPosition PreviousPosition { get; set; } = new();
    public PtzPosition CurrentPosition { get; set; } = new();
    public PtzMoveType MoveType { get; set; }
    public string? PresetName { get; set; } // If moved to preset

    public PtzMovedEvent()
    {
        EventType = "PtzMoved";
    }
}

public class PtzPresetEvent : BaseEvent
{
    public PtzPresetAction Action { get; set; }
    public PtzPreset Preset { get; set; } = new();

    public PtzPresetEvent()
    {
        EventType = "PtzPreset";
    }
}

public enum PtzPresetAction
{
    Created,
    Updated,
    Deleted,
    Accessed
}

// Motion Detection Events
public class MotionDetectedEvent : BaseEvent
{
    public MotionRegion[] Regions { get; set; } = Array.Empty<MotionRegion>();
    public float Confidence { get; set; }
    public DateTime DetectionTime { get; set; }
    public string? SnapshotPath { get; set; }

    public MotionDetectedEvent()
    {
        EventType = "MotionDetected";
    }
}

public class MotionRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Confidence { get; set; }
}

// System Events
public class CameraControllerStartedEvent : BaseEvent
{
    public string Version { get; set; } = string.Empty;
    public int ManagedCameraCount { get; set; }
    public string[] EnabledFeatures { get; set; } = Array.Empty<string>();

    public CameraControllerStartedEvent()
    {
        EventType = "CameraControllerStarted";
    }
}

public class CameraControllerStoppedEvent : BaseEvent
{
    public string Reason { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }

    public CameraControllerStoppedEvent()
    {
        EventType = "CameraControllerStopped";
    }
}

// ONVIF Specific Events
public class OnvifEventReceived : BaseEvent
{
    public OnvifEvent OnvifEvent { get; set; } = new();
    public string EventCategory { get; set; } = string.Empty; // Motion, IO, PTZ, etc.

    public OnvifEventReceived()
    {
        EventType = "OnvifEventReceived";
    }
}