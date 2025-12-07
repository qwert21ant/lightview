using Lightview.Shared.Contracts;

namespace CameraController.Contracts.Models;

/// <summary>
/// Event arguments for camera status changes
/// </summary>
public class CameraStatusChangedEventArgs : EventArgs
{
    public CameraStatus PreviousStatus { get; set; }
    public CameraStatus CurrentStatus { get; set; }
    public string? Reason { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Event arguments for PTZ position changes
/// </summary>
public class PtzPositionChangedEventArgs : EventArgs
{
    public PtzPosition PreviousPosition { get; set; } = new();
    public PtzPosition CurrentPosition { get; set; } = new();
    public PtzMoveType MoveType { get; set; }
}

/// <summary>
/// Event arguments for camera health changes
/// </summary>
public class CameraHealthChangedEventArgs : EventArgs
{
    public CameraHealthStatus PreviousHealth { get; set; } = new();
    public CameraHealthStatus CurrentHealth { get; set; } = new();
}

/// <summary>
/// Event arguments for camera snapshot captured events
/// </summary>
public class CameraSnapshotCapturedEventArgs : EventArgs
{
    /// <summary>
    /// Camera ID that captured the snapshot
    /// </summary>
    public required Guid CameraId { get; set; }
    
    /// <summary>
    /// Snapshot image data (JPEG bytes)
    /// </summary>
    public required byte[] SnapshotData { get; set; }
    
    /// <summary>
    /// Profile token used for capture
    /// </summary>
    public string? ProfileToken { get; set; }
    
    /// <summary>
    /// Time taken to capture the snapshot
    /// </summary>
    public TimeSpan CaptureTime { get; set; }
}