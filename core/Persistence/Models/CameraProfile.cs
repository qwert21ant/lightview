using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models;

public class CameraProfile
{
    [Key]
    public Guid Id { get; set; }
    public Guid CameraId { get; set; }
    public required string Token { get; set; }
    public required string Name { get; set; }
    public bool IsMainStream { get; set; }
    public string? RtspUri { get; set; }
    public string? WebRtcUri { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string VideoConfigJson { get; set; } = "{}";
    
    [Column(TypeName = "jsonb")]
    public string AudioConfigJson { get; set; } = "{}";
    
    // Navigation property
    public Camera Camera { get; set; } = null!;
}