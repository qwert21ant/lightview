using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lightview.Shared.Contracts;

namespace Persistence.Models;

public class CameraMetadata
{
    [Key]
    public Guid CameraId { get; set; }
    public int Status { get; set; } = (int)CameraStatus.Offline;
    public DateTime LastConnectedAt { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? CapabilitiesJson { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? DeviceInfoJson { get; set; }
    
    // Navigation property
    public Camera Camera { get; set; } = null!;
}