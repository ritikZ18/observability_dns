using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObservabilityDns.Domain.Entities;

[Table("incidents")]
public class Incident
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("domain_id")]
    public Guid DomainId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("check_type")]
    public string CheckType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("severity")]
    public string Severity { get; set; } = "MEDIUM";

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "OPEN";

    [Column("reason", TypeName = "text")]
    public string? Reason { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DomainId")]
    public virtual Domain Domain { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
