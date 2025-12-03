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