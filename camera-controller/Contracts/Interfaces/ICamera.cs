using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Represents a camera connection and provides methods for camera operations
/// </summary>
public interface ICamera : IDisposable, ICameraHealthCheck
{
    /// <summary>
    /// Unique identifier of the camera
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Current connection status
    /// </summary>
    CameraStatus Status { get; }
    
    /// <summary>
    /// Camera configuration
    /// </summary>
    Camera Configuration { get; }
    
    /// <summary>
    /// Camera capabilities (available after successful connection)
    /// </summary>
    CameraCapabilities? Capabilities { get; }
    
    /// <summary>
    /// Available stream profiles
    /// </summary>
    IReadOnlyList<CameraProfile> Profiles { get; }
    
    /// <summary>
    /// PTZ control interface (null if PTZ is not supported)
    /// </summary>
    IPtzControl? PtzControl { get; }
    
    /// <summary>
    /// Event raised when camera status changes
    /// </summary>
    event EventHandler<CameraStatusChangedEventArgs> StatusChanged;
    
    /// <summary>
    /// Updates the camera status (used by monitoring service)
    /// </summary>
    void UpdateStatus(CameraStatus status, string? reason = null);
    
    /// <summary>
    /// Connect to the camera (performs all health checks during connection)
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnect from the camera
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Test if camera is reachable
    /// </summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current device information
    /// </summary>
    Task<OnvifDeviceInfo?> GetDeviceInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available stream profiles
    /// </summary>
    Task<List<CameraProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get stream URI for specified profile
    /// </summary>
    Task<Uri?> GetStreamUriAsync(string profileToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Capture snapshot from camera
    /// </summary>
    Task<byte[]?> CaptureSnapshotAsync(string? profileToken = null, CancellationToken cancellationToken = default);
    
    // Image Settings
    
    /// <summary>
    /// Get current image settings
    /// </summary>
    Task<ImageSettings?> GetImageSettingsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update image settings
    /// </summary>
    Task<bool> SetImageSettingsAsync(ImageSettings settings, CancellationToken cancellationToken = default);
}
