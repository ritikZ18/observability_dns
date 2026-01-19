using System.ComponentModel.DataAnnotations;
using ObservabilityDns.Contracts.Enums;

namespace ObservabilityDns.Contracts.DTOs;

public class CreateAlertRuleRequest
{
    [Required]
    public CheckType CheckType { get; set; }

    [Required]
    [MaxLength(100)]
    public string TriggerCondition { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
