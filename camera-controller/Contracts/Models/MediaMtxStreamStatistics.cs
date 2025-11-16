namespace CameraController.Contracts.Models;

/// <summary>
/// Statistics for a MediaMTX media stream
/// </summary>
public class MediaMtxStreamStatistics
{
    /// <summary>
    /// Camera ID
    /// </summary>
    public Guid CameraId { get; set; }

    /// <summary>
    /// Stream path in MediaMTX
    /// </summary>
    public string StreamPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the stream is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of clients currently watching the stream
    /// </summary>
    public int ClientCount { get; set; }

    /// <summary>
    /// Bytes received from the source
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Bytes sent to clients
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// Stream start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Stream resolution (e.g., "1920x1080")
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Frame rate (FPS)
    /// </summary>
    public double? FrameRate { get; set; }

    /// <summary>
    /// Bitrate in bits per second
    /// </summary>
    public long? Bitrate { get; set; }

    /// <summary>
    /// Video codec (e.g., "H.264", "H.265")
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Audio codec (e.g., "AAC", "G.711")
    /// </summary>
    public string? AudioCodec { get; set; }
}