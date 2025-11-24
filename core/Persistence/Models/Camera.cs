using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lightview.Shared.Contracts;

namespace Persistence.Models;

public class Camera
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Protocol { get; set; } = (int)CameraProtocol.Onvif;
    public int Status { get; set; } = (int)CameraStatus.Offline;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastConnectedAt { get; set; }
    
    // JSON columns for complex types
    [Column(TypeName = "jsonb")]
    public string? CapabilitiesJson { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? ProfilesJson { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? DeviceInfoJson { get; set; }
}
