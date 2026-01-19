using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ObservabilityDns.Domain.Entities;

[Table("probe_runs")]
public class ProbeRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("domain_id")]
    public Guid DomainId { get; set; }

    [Required]
    [Column("check_id")]
    public Guid CheckId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("check_type")]
    public string CheckType { get; set; } = string.Empty;

    [Column("success")]
    public bool Success { get; set; }

    [MaxLength(100)]
    [Column("error_code")]
    public string? ErrorCode { get; set; }

    [Column("error_message", TypeName = "text")]
    public string? ErrorMessage { get; set; }

    [Column("dns_ms")]
    public int? DnsMs { get; set; }

    [Column("tls_ms")]
    public int? TlsMs { get; set; }

    [Column("ttfb_ms")]
    public int? TtfbMs { get; set; }

    [Required]
    [Column("total_ms")]
    public int TotalMs { get; set; }

    [Column("status_code")]
    public int? StatusCode { get; set; }

    [Column("records_snapshot", TypeName = "jsonb")]
    public JsonDocument? RecordsSnapshot { get; set; }

    [Column("certificate_info", TypeName = "jsonb")]
    public JsonDocument? CertificateInfo { get; set; }

    [Required]
    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Required]
    [Column("completed_at")]
    public DateTime CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DomainId")]
    public virtual Domain Domain { get; set; } = null!;

    [ForeignKey("CheckId")]
    public virtual Check Check { get; set; } = null!;
}
