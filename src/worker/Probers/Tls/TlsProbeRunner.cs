using ObservabilityDns.Worker.Probers;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace ObservabilityDns.Worker.Probers.Tls;

public class TlsProbeRunner : ITlsProbeRunner
{
    private readonly ILogger<TlsProbeRunner> _logger;

    public TlsProbeRunner(ILogger<TlsProbeRunner> logger)
    {
        _logger = logger;
    }

    public async Task<ProbeResult> RunProbeAsync(string target, CancellationToken cancellationToken = default)
    {
        // Extract host from URL if needed
        var host = ExtractHost(target);
        return await CheckCertificateAsync(host, 443, cancellationToken);
    }

    public async Task<TlsProbeResult> CheckCertificateAsync(string host, int port = 443, CancellationToken cancellationToken = default)
    {
        var result = new TlsProbeResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port);

            X509Certificate2? certificate = null;
            var validationErrors = SslPolicyErrors.None;

            using var sslStream = new SslStream(
                tcpClient.GetStream(),
                false,
                (sender, cert, chain, errors) =>
                {
                    certificate = cert as X509Certificate2;
                    validationErrors = errors;
                    return true; // Accept to get cert info
                });

            await sslStream.AuthenticateAsClientAsync(host);

            if (certificate != null)
            {
                result.IsValid = validationErrors == SslPolicyErrors.None;
                result.Issuer = certificate.Issuer;
                result.Subject = certificate.Subject;
                result.NotBefore = certificate.NotBefore;
                result.NotAfter = certificate.NotAfter;

                if (certificate.NotAfter > DateTime.Now)
                {
                    result.DaysUntilExpiry = (int)(certificate.NotAfter - DateTime.Now).TotalDays;
                }
                else
                {
                    result.DaysUntilExpiry = null;
                    result.IsValid = false;
                }

                // Extract SANs
                result.SubjectAlternativeNames = ExtractSanNames(certificate);

                // Create JSON snapshot
                var certInfo = new
                {
                    issuer = result.Issuer,
                    subject = result.Subject,
                    notBefore = result.NotBefore,
                    notAfter = result.NotAfter,
                    daysUntilExpiry = result.DaysUntilExpiry,
                    subjectAlternativeNames = result.SubjectAlternativeNames,
                    isValid = result.IsValid
                };
                result.CertificateInfo = JsonDocument.Parse(JsonSerializer.Serialize(certInfo));
            }

            result.Success = certificate != null;
            if (!result.Success)
            {
                result.ErrorCode = "NO_CERTIFICATE";
                result.ErrorMessage = "No certificate found";
            }
            else if (!result.IsValid)
            {
                result.ErrorCode = "INVALID_CERTIFICATE";
                result.ErrorMessage = validationErrors.ToString();
            }
        }
        catch (SocketException ex)
        {
            result.Success = false;
            result.ErrorCode = "CONNECTION_FAILED";
            result.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "TLS connection failed for {Host}:{Port}", host, port);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorCode = "UNKNOWN";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error checking TLS certificate for {Host}:{Port}", host, port);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private List<string> ExtractSanNames(X509Certificate2 certificate)
    {
        var sans = new List<string>();
        try
        {
            var sanExtension = certificate.Extensions["2.5.29.17"]; // SAN OID
            if (sanExtension != null)
            {
                var asnData = sanExtension.Format(false);
                // Parse DNS names from ASN.1 format (simplified)
                var lines = asnData.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("DNS Name="))
                    {
                        var dnsName = line.Split("DNS Name=")[1].Trim();
                        sans.Add(dnsName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract SAN names from certificate");
        }
        return sans;
    }

    private string ExtractHost(string input)
    {
        var host = input.Trim().ToLowerInvariant();
        if (host.StartsWith("http://"))
            host = host.Substring(7);
        if (host.StartsWith("https://"))
            host = host.Substring(8);
        if (host.StartsWith("www."))
            host = host.Substring(4);
        var slashIndex = host.IndexOf('/');
        if (slashIndex >= 0)
            host = host.Substring(0, slashIndex);
        var colonIndex = host.IndexOf(':');
        if (colonIndex >= 0)
            host = host.Substring(0, colonIndex);
        return host;
    }
}
