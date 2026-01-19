using ObservabilityDns.Contracts.Enums;
using System.Text.Json;

namespace ObservabilityDns.Worker.Probers;

public interface IProbeRunner
{
    Task<ProbeResult> RunProbeAsync(string target, CancellationToken cancellationToken = default);
}

public interface IDnsProbeRunner : IProbeRunner
{
    Task<DnsProbeResult> ResolveAsync(string domain, CancellationToken cancellationToken = default);
}

public interface ITlsProbeRunner : IProbeRunner
{
    Task<TlsProbeResult> CheckCertificateAsync(string host, int port = 443, CancellationToken cancellationToken = default);
}

public interface IHttpProbeRunner : IProbeRunner
{
    Task<HttpProbeResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default);
}

public abstract class ProbeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DnsProbeResult : ProbeResult
{
    public List<string> IpAddresses { get; set; } = new();
    public List<DnsRecord> Records { get; set; } = new();
    public JsonDocument? RecordsSnapshot { get; set; }
}

public class DnsRecord
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Ttl { get; set; }
}

public class TlsProbeResult : ProbeResult
{
    public bool IsValid { get; set; }
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public List<string> SubjectAlternativeNames { get; set; } = new();
    public JsonDocument? CertificateInfo { get; set; }
}

public class HttpProbeResult : ProbeResult
{
    public int? StatusCode { get; set; }
    public int? TtfbMs { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}
