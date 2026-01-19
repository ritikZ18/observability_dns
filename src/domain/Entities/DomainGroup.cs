using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObservabilityDns.Domain.Entities;

[Table("domain_groups")]
public class DomainGroup
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(7)]
    [Column("color")]
    public string? Color { get; set; } // Hex color code (e.g., #3B82F6)

    [MaxLength(50)]
    [Column("icon")]
    public string? Icon { get; set; } // Icon identifier (e.g., "briefcase", "graduation-cap")

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Domain> Domains { get; set; } = new List<Domain>();
}
