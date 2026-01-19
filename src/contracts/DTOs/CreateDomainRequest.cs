using System.ComponentModel.DataAnnotations;

namespace ObservabilityDns.Contracts.DTOs;

public class CreateDomainRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 15)]
    public int IntervalMinutes { get; set; } = 5;

    public bool Enabled { get; set; } = true;

    [MaxLength(50)]
    public string? Icon { get; set; }

    public Guid? GroupId { get; set; }
}
