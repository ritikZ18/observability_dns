using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ObservabilityDns.Domain.Entities;

[Table("checks")]
public class Check
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

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("configuration", TypeName = "jsonb")]
    public JsonDocument? Configuration { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DomainId")]
    public virtual Domain Domain { get; set; } = null!;

    public virtual ICollection<ProbeRun> ProbeRuns { get; set; } = new List<ProbeRun>();
}
