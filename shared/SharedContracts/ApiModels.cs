namespace Lightview.Shared.Contracts;

// API Request/Response Models
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApiResponse : ApiResponse<object>
{
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// Camera API Models
public class CreateCameraRequest
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public CameraCredentials Credentials { get; set; } = new();
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Onvif;
    public bool AutoConnect { get; set; } = true;
}

public class UpdateCameraRequest
{
    public string? Name { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public CameraCredentials? Credentials { get; set; }
    public CameraProtocol? Protocol { get; set; }
}

public class CameraListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public CameraStatus Status { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public int StreamCount { get; set; }
}

public class CameraDetailResponse : CameraListResponse
{
    public int Port { get; set; }
    public CameraProtocol Protocol { get; set; }
    public CameraCapabilities? Capabilities { get; set; }
    public List<CameraProfile> Profiles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }
}

// Stream API Models
public class StartStreamRequest
{
    public string? ProfileToken { get; set; } // Optional, uses main profile if not specified
    public StreamSettings? Settings { get; set; }
}

public class StreamResponse
{
    public string StreamPath { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public string ProfileToken { get; set; } = string.Empty;
    public StreamStatus Status { get; set; } = new();
}

// PTZ API Models
public class PtzMoveResponse
{
    public PtzPosition NewPosition { get; set; } = new();
    public bool IsMoving { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PtzStatusResponse
{
    public PtzPosition CurrentPosition { get; set; } = new();
    public bool IsMoving { get; set; }
    public DateTime LastMoveTime { get; set; }
    public string? CurrentPreset { get; set; }
}

// Settings API Models
public class UpdateImageSettingsRequest
{
    public float? Brightness { get; set; }
    public float? Contrast { get; set; }
    public float? Saturation { get; set; }
    public float? Sharpness { get; set; }
    public FocusSettings? Focus { get; set; }
    public IrSettings? IrCut { get; set; }
    public WhiteBalanceSettings? WhiteBalance { get; set; }
}

// Health Check Models
public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class ComponentHealth
{
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

// Snapshot Models
public class SnapshotRequest
{
    public string? ProfileToken { get; set; }
    public Resolution? Resolution { get; set; }
    public int Quality { get; set; } = 90; // JPEG quality 1-100
}

public class SnapshotResponse
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public Resolution Resolution { get; set; } = new();
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}