using ObservabilityDns.Contracts.Enums;

namespace ObservabilityDns.Contracts.DTOs;

public class DomainDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CheckDto> Checks { get; set; } = new();
    public List<ProbeRunDto> RecentRuns { get; set; } = new();
    public List<IncidentDto> OpenIncidents { get; set; } = new();
}

public class CheckDto
{
    public Guid Id { get; set; }
    public CheckType CheckType { get; set; }
    public bool Enabled { get; set; }
}
