using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;

namespace CameraControllerConnector.Interfaces;

/// <summary>
/// Interface for communicating with the camera controller service
/// </summary>
public interface ICameraControllerClient
{
    /// <summary>
    /// Get all cameras from the controller
    /// </summary>
    Task<List<CameraStatusResponse>> GetAllCamerasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific camera by ID
    /// </summary>
    Task<CameraStatusResponse?> GetCameraAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new camera to the controller
    /// </summary>
    Task<CameraStatusResponse> AddCameraAsync(Guid id, AddCameraRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update camera settings
    /// </summary>
    Task<CameraStatusResponse> UpdateCameraAsync(Guid cameraId, UpdateCameraRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a camera from the controller
    /// </summary>
    Task<bool> RemoveCameraAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect to a camera
    /// </summary>
    Task<bool> ConnectCameraAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from a camera
    /// </summary>
    Task<bool> DisconnectCameraAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get camera health status
    /// </summary>
    Task<CameraHealthStatus?> GetCameraHealthAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture a snapshot from the camera
    /// </summary>
    Task<byte[]?> CaptureSnapshotAsync(Guid cameraId, string? profileToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest snapshot from the camera without capturing a new one
    /// </summary>
    Task<(byte[]? ImageData, DateTime? CapturedAt, string? ProfileToken)> GetLatestSnapshotAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get WebRTC stream URL for a camera
    /// </summary>
    Task<string?> GetWebRtcStreamUrlAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move PTZ camera
    /// </summary>
    Task<PtzMoveResponse> MovePtzAsync(Guid cameraId, PtzMoveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop PTZ movement
    /// </summary>
    Task<bool> StopPtzAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Go to PTZ preset
    /// </summary>
    Task<bool> GotoPtzPresetAsync(Guid cameraId, string presetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the camera controller service is healthy
    /// </summary>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
