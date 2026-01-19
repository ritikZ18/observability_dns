namespace ObservabilityDns.Contracts.DTOs;

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool Enabled { get; set; }
    public int DomainCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GroupDetailDto : GroupDto
{
    public List<DomainDto> Domains { get; set; } = new();
    public GroupStatisticsDto? Statistics { get; set; }
}

public class GroupStatisticsDto
{
    public int TotalDomains { get; set; }
    public int EnabledDomains { get; set; }
    public int TotalProbeRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public int FailedRuns { get; set; }
    public double AverageLatency { get; set; }
    public int OpenIncidents { get; set; }
    public DateTime? LastProbeRun { get; set; }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool Enabled { get; set; } = true;
}

public class UpdateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool? Enabled { get; set; }
}
