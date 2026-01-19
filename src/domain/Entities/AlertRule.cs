using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObservabilityDns.Domain.Entities;

[Table("alert_rules")]
public class AlertRule
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
    [MaxLength(100)]
    [Column("trigger_condition")]
    public string TriggerCondition { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DomainId")]
    public virtual Domain Domain { get; set; } = null!;
}
