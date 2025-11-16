using Lightview.Shared.Contracts;

namespace CameraController.Contracts.Models;

/// <summary>
/// DTO for camera creation requests
/// </summary>
public class CreateCameraDto
{
    public string Name { get; set; } = string.Empty;
    public required Uri Url { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Onvif;
    public bool AutoConnect { get; set; } = true;
    public CameraMonitoringConfig? MonitoringConfig { get; set; }
}

/// <summary>
/// DTO for camera update requests
/// </summary>
public class UpdateCameraDto
{
    public string? Name { get; set; }
    public Uri? Url { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public CameraProtocol? Protocol { get; set; }
}

/// <summary>
/// DTO for camera status response
/// </summary>
public class CameraStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public required Uri Url { get; set; }
    public CameraStatus Status { get; set; }
    public bool IsMonitoring { get; set; }
    public CameraHealthStatus? Health { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public CameraDeviceInfo? DeviceInfo { get; set; }
    public CameraCapabilities? Capabilities { get; set; }
    public int ProfileCount { get; set; }
}