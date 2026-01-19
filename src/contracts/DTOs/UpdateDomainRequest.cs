using System.ComponentModel.DataAnnotations;

namespace ObservabilityDns.Contracts.DTOs;

public class UpdateDomainRequest
{
    public bool? Enabled { get; set; }

    [Range(1, 15)]
    public int? IntervalMinutes { get; set; }
}
