// TODO: Implement IProbeRunner interface
//
// This interface defines the contract for all probe runners (DNS, TLS, HTTP)
//
// Example structure:
// public interface IProbeRunner
// {
//     Task<ProbeResult> RunProbeAsync(string target, CancellationToken cancellationToken = default);
// }
//
// public interface IDnsProbeRunner : IProbeRunner
// {
//     Task<DnsProbeResult> ResolveAsync(string domain, DnsRecordType recordType, CancellationToken cancellationToken = default);
// }
//
// public interface ITlsProbeRunner : IProbeRunner
// {
//     Task<TlsProbeResult> CheckCertificateAsync(string host, int port, CancellationToken cancellationToken = default);
// }
//
// public interface IHttpProbeRunner : IProbeRunner
// {
//     Task<HttpProbeResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default);
// }
//
// public abstract class ProbeResult
// {
//     public bool Success { get; set; }
//     public string ErrorMessage { get; set; }
//     public TimeSpan Duration { get; set; }
//     public DateTime Timestamp { get; set; }
// }
