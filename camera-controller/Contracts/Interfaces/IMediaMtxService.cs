using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Service for managing MediaMTX streaming server integration
/// </summary>
public interface IMediaMtxService
{
    /// <summary>
    /// Configure an RTSP input stream from a camera in MediaMTX
    /// </summary>
    /// <param name="camera">Camera configuration</param>
    /// <param name="rtspUrl">RTSP URL from the camera</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream path that can be used for WebRTC access</returns>
    Task<string> ConfigureRtspInputAsync(Camera camera, string rtspUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove stream configuration when camera is disconnected
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveStreamConfigurationAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get WebRTC URL for client streaming
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebRTC URL for client access</returns>
    Task<string> GetWebRtcUrlAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if stream is active and healthy
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stream is active</returns>
    Task<bool> IsStreamActiveAsync(Guid cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stream statistics
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream statistics</returns>
    Task<MediaMtxStreamStatistics?> GetStreamStatisticsAsync(Guid cameraId, CancellationToken cancellationToken = default);
}