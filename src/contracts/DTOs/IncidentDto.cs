using ObservabilityDns.Contracts.Enums;

namespace ObservabilityDns.Contracts.DTOs;

public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public CheckType CheckType { get; set; }
    public Severity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
