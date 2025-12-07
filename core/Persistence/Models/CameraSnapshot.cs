using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models;

/// <summary>
/// Entity for storing camera snapshots in the database
/// </summary>
public class CameraSnapshot
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid CameraId { get; set; }
    
    /// <summary>
    /// JPEG image data
    /// </summary>
    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// Profile token used to capture the snapshot
    /// </summary>
    [MaxLength(100)]
    public string? ProfileToken { get; set; }
    
    /// <summary>
    /// When the snapshot was captured
    /// </summary>
    [Required]
    public DateTime CapturedAt { get; set; }
    
    /// <summary>
    /// Size of the image in bytes
    /// </summary>
    [Required]
    public int FileSize { get; set; }
    
    /// <summary>
    /// Navigation property to the camera
    /// </summary>
    [ForeignKey(nameof(CameraId))]
    public Camera Camera { get; set; } = null!;
}
