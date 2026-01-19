using System.Text.Json;

namespace ObservabilityDns.Contracts.DTOs;

public class WebsiteInfoDto
{
    public string Domain { get; set; } = string.Empty;
    public List<string> IpAddresses { get; set; } = new();
    public List<DnsRecordDto> DnsRecords { get; set; } = new();
    public TlsInfoDto? TlsInfo { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

public class DnsRecordDto
{
    public string Type { get; set; } = string.Empty; // A, AAAA, CNAME, etc.
    public string Value { get; set; } = string.Empty;
    public int? Ttl { get; set; }
}

public class TlsInfoDto
{
    public bool IsValid { get; set; }
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public List<string> SubjectAlternativeNames { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
