namespace CameraController.Contracts.Models;

/// <summary>
/// Configuration for MediaMTX integration
/// </summary>
public class MediaMtxConfiguration
{
    /// <summary>
    /// MediaMTX API endpoint URL (e.g., "http://localhost:9997/v3")
    /// </summary>
    public string ApiUrl { get; set; } = "http://localhost:9997/v3";

    /// <summary>
    /// WebRTC server URL for client connections (e.g., "http://localhost:8889")
    /// </summary>
    public string WebRtcUrl { get; set; } = "http://localhost:8889";

    /// <summary>
    /// RTSP server URL for local stream access (e.g., "rtsp://localhost:8554")
    /// </summary>
    public string RtspUrl { get; set; } = "rtsp://localhost:8554";

    /// <summary>
    /// Username for MediaMTX API authentication (if required)
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Password for MediaMTX API authentication (if required)
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// HTTP client timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Stream path prefix for cameras
    /// </summary>
    public string StreamPathPrefix { get; set; } = "camera";

    /// <summary>
    /// Enable authentication for streams
    /// </summary>
    public bool EnableAuthentication { get; set; } = false;

    /// <summary>
    /// Default username for stream authentication
    /// </summary>
    public string? DefaultUsername { get; set; }

    /// <summary>
    /// Default password for stream authentication
    /// </summary>
    public string? DefaultPassword { get; set; }
}