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
    public CameraStatus Status { get; set; } = CameraStatus.Disabled;
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
    Disabled,
    Offline,
    Online,
    Connecting,
    Error,
    Maintenance
}

public enum CameraProtocol
{
    Onvif,
    Rtsp,
    Http
}
