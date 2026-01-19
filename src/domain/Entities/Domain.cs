using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObservabilityDns.Domain.Entities;

[Table("domains")]
public class Domain
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("interval_minutes")]
    public int IntervalMinutes { get; set; } = 5;

    [MaxLength(50)]
    [Column("icon")]
    public string? Icon { get; set; } // Icon identifier (e.g., "globe", "briefcase", "graduation-cap")

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Group relationship (optional)
    [Column("group_id")]
    public Guid? GroupId { get; set; }

    // Navigation properties
    public virtual DomainGroup? Group { get; set; }
    public virtual ICollection<Check> Checks { get; set; } = new List<Check>();
    public virtual ICollection<ProbeRun> ProbeRuns { get; set; } = new List<ProbeRun>();
    public virtual ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
