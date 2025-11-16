namespace Lightview.Shared.Contracts;

// ONVIF Specific Models
public class OnvifDeviceInfo
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public List<string> XAddrs { get; set; } = new();
    public int MetadataVersion { get; set; }
}

public class OnvifService
{
    public string Namespace { get; set; } = string.Empty;
    public string XAddr { get; set; } = string.Empty;
    public OnvifCapabilities Capabilities { get; set; } = new();
    public string Version { get; set; } = string.Empty;
}

public class OnvifCapabilities
{
    public bool Analytics { get; set; }
    public bool Device { get; set; }
    public bool Events { get; set; }
    public bool Imaging { get; set; }
    public bool Media { get; set; }
    public bool PTZ { get; set; }
}

public class StreamUri
{
    public string Uri { get; set; } = string.Empty;
    public string ProfileToken { get; set; } = string.Empty;
    public StreamProtocol Protocol { get; set; }
    public bool InvalidAfterConnect { get; set; }
    public bool InvalidAfterReboot { get; set; }
    public TimeSpan Timeout { get; set; }
}

public class OnvifEvent
{
    public string Topic { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public object? Data { get; set; }
}

// Discovery Models
public class CameraDiscoveryResult
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> ServiceUrls { get; set; } = new();
    public OnvifDeviceInfo DeviceInfo { get; set; } = new();
    public CameraCapabilities Capabilities { get; set; } = new();
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}

public class CameraDiscoveryRequest
{
    public string? NetworkInterface { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool IncludeCapabilities { get; set; } = true;
}