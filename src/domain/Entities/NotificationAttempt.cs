using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObservabilityDns.Domain.Entities;

[Table("notification_attempts")]
public class NotificationAttempt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("notification_id")]
    public Guid NotificationId { get; set; }

    [Required]
    [Column("attempt_number")]
    public int AttemptNumber { get; set; }

    [Column("error_message", TypeName = "text")]
    public string? ErrorMessage { get; set; }

    [Column("attempted_at")]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("NotificationId")]
    public virtual Notification Notification { get; set; } = null!;
}
