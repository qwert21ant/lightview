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
