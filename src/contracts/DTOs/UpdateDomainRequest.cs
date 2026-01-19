using System.ComponentModel.DataAnnotations;

namespace ObservabilityDns.Contracts.DTOs;

public class UpdateDomainRequest
{
    public bool? Enabled { get; set; }

    [Range(1, 15)]
    public int? IntervalMinutes { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public Guid? GroupId { get; set; }
}
