namespace Lightview.Shared.Contracts.InternalApi;

/// <summary>
/// Request to add a new camera to the camera controller
/// </summary>
public class AddCameraRequest
{
    public required string Name { get; set; }
    public required Uri Url { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Onvif;
    public bool AutoConnect { get; set; } = true;
    public CameraMonitoringConfig? MonitoringConfig { get; set; }
}

/// <summary>
/// Request to update camera settings in the camera controller
/// </summary>
public class UpdateCameraRequest
{
    public string? Name { get; set; }
    public Uri? Url { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public CameraProtocol? Protocol { get; set; }
}

/// <summary>
/// Response with camera status information from camera controller
/// </summary>
public class CameraStatusResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required Uri Url { get; set; }
    public CameraStatus Status { get; set; }
    public bool IsMonitoring { get; set; }
    public CameraHealthStatus? Health { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public CameraDeviceInfo? DeviceInfo { get; set; }
    public CameraCapabilities? Capabilities { get; set; }
    public int ProfileCount { get; set; }
}

/// <summary>
/// Request to get WebRTC stream URL
/// </summary>
public class GetStreamRequest
{
    public Guid CameraId { get; set; }
    public string? ProfileToken { get; set; }
}

/// <summary>
/// Response with stream URL
/// </summary>
public class StreamUrlResponse
{
    public required string Url { get; set; }
    public string? Protocol { get; set; }
}

/// <summary>
/// Camera monitoring configuration
/// </summary>
public class CameraMonitoringConfig
{
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Interval between health checks
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Timeout for individual health checks
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Number of consecutive failures before marking as unhealthy
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
    
    /// <summary>
    /// Number of consecutive successes before marking as healthy again
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
    
    /// <summary>
    /// Whether to attempt automatic reconnection on failures
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
    
    /// <summary>
    /// Maximum number of reconnection attempts
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;
    
    /// <summary>
    /// Delay between reconnection attempts
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Whether to publish health events to RabbitMQ
    /// </summary>
    public bool PublishHealthEvents { get; set; } = true;
    
    /// <summary>
    /// Whether to publish detailed statistics
    /// </summary>
    public bool PublishStatistics { get; set; } = false;
}
