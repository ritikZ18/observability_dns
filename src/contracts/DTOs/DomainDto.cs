namespace ObservabilityDns.Contracts.DTOs;

public class DomainDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? GroupColor { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
