using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Service for managing MediaMTX streaming server integration
/// </summary>
public interface IMediaMtxService
{
    /// <summary>
    /// Configure stream profiles for a camera in MediaMTX
    /// </summary>
    /// <param name="camera">Camera configuration</param>
    /// <param name="profiles">Camera profiles with RTSP URIs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated profiles with WebRTC URIs</returns>
    Task<List<CameraProfile>> ConfigureStreamProfilesAsync(Camera camera, List<CameraProfile> profiles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all stream configurations for a camera when disconnected
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAllStreamConfigurationsAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get WebRTC URL for a specific camera profile
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="profileToken">Profile token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebRTC URL for client access</returns>
    Task<Uri?> GetWebRtcUrlAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific profile stream is active and healthy
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="profileToken">Profile token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stream is active</returns>
    Task<bool> IsStreamActiveAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stream statistics for a specific profile
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="profileToken">Profile token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream statistics</returns>
    Task<MediaMtxStreamStatistics?> GetStreamStatisticsAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default);
}