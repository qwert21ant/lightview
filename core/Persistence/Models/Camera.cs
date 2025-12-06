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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public CameraMetadata? Metadata { get; set; }
    public ICollection<CameraProfile> Profiles { get; set; } = new List<CameraProfile>();
}
