using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Interface for PTZ (Pan-Tilt-Zoom) camera control operations
/// </summary>
public interface IPtzControl
{
    /// <summary>
    /// Current PTZ position
    /// </summary>
    PtzPosition? CurrentPosition { get; }
    
    /// <summary>
    /// Whether PTZ operations are supported
    /// </summary>
    bool IsSupported { get; }
    
    /// <summary>
    /// Event raised when PTZ position changes
    /// </summary>
    event EventHandler<PtzPositionChangedEventArgs> PositionChanged;
    
    /// <summary>
    /// Move camera to absolute position
    /// </summary>
    Task<bool> MoveAbsoluteAsync(PtzPosition position, PtzSpeed? speed = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Move camera relative to current position
    /// </summary>
    Task<bool> MoveRelativeAsync(PtzPosition movement, PtzSpeed? speed = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start continuous movement
    /// </summary>
    Task<bool> MoveContinuousAsync(PtzSpeed speed, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop PTZ movement
    /// </summary>
    Task<bool> StopMovementAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current PTZ position
    /// </summary>
    Task<PtzPosition?> GetPositionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available presets
    /// </summary>
    Task<List<PtzPreset>> GetPresetsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Go to preset position
    /// </summary>
    Task<bool> GotoPresetAsync(int presetId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set current position as preset
    /// </summary>
    Task<bool> SetPresetAsync(string presetName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove preset
    /// </summary>
    Task<bool> RemovePresetAsync(int presetId, CancellationToken cancellationToken = default);
}