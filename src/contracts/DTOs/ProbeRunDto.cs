using ObservabilityDns.Contracts.Enums;
using System.Text.Json;

namespace ObservabilityDns.Contracts.DTOs;

public class ProbeRunDto
{
    public Guid Id { get; set; }
    public Guid DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public CheckType CheckType { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DnsMs { get; set; }
    public int? TlsMs { get; set; }
    public int? TtfbMs { get; set; }
    public int TotalMs { get; set; }
    public int? StatusCode { get; set; }
    public JsonDocument? RecordsSnapshot { get; set; }
    public JsonDocument? CertificateInfo { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
