namespace Lightview.Shared.Contracts;

// Camera Management Models
public class Camera
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public required Uri Url { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Onvif;
    public CameraStatus Status { get; set; } = CameraStatus.Offline;
    public CameraCapabilities? Capabilities { get; set; }
    public List<CameraProfile> Profiles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastConnectedAt { get; set; }
    public CameraDeviceInfo? DeviceInfo { get; set; }
}

public class CameraDeviceInfo
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }
}

public class CameraCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public enum CameraStatus
{
    Offline,     // Camera added but no connection attempts made
    Connecting,  // Camera-controller attempting to connect
    Online,      // Camera connected successfully, no health check failures
    Degraded,    // Camera connected but health checks failing
    Error        // Connection failed or unrecoverable error
}

public enum CameraProtocol
{
    Onvif,
    Rtsp
}

// Health Check Models
public class HealthCheckResult
{
    public bool IsSuccessful { get; set; }
    public string CheckName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class CameraHealthCheckResults
{
    public bool OverallHealthy { get; set; }
    public List<HealthCheckResult> Results { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalResponseTime { get; set; }
}
