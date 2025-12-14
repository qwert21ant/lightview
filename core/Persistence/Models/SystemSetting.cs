using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models;

public class SystemSetting
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "jsonb")]
    public string Value { get; set; } = "{}";
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}