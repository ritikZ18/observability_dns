using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ObservabilityDns.Domain.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("domain_id")]
    public Guid DomainId { get; set; }

    [Column("incident_id")]
    public Guid? IncidentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("channel")]
    public string Channel { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("destination")]
    public string Destination { get; set; } = string.Empty;

    [Required]
    [Column("payload", TypeName = "jsonb")]
    public JsonDocument Payload { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    // Navigation properties
    [ForeignKey("DomainId")]
    public virtual Domain Domain { get; set; } = null!;

    [ForeignKey("IncidentId")]
    public virtual Incident? Incident { get; set; }

    public virtual ICollection<NotificationAttempt> NotificationAttempts { get; set; } = new List<NotificationAttempt>();
}
