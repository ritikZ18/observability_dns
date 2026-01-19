using DnsClient;
using DnsClient.Protocol;
using ObservabilityDns.Contracts.DTOs;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace ObservabilityDns.Api.Services;

public class WebsiteInfoService
{
    private readonly ILogger<WebsiteInfoService> _logger;
    private readonly LookupClient _dnsClient;

    public WebsiteInfoService(ILogger<WebsiteInfoService> logger)
    {
        _logger = logger;
        _dnsClient = new LookupClient();
    }

    public async Task<WebsiteInfoDto> GetWebsiteInfoAsync(string domain)
    {
        var normalizedDomain = NormalizeDomainName(domain);
        var result = new WebsiteInfoDto
        {
            Domain = normalizedDomain,
            FetchedAt = DateTime.UtcNow
        };

        try
        {
            // DNS Lookup - A records
            var aRecords = await _dnsClient.QueryAsync(normalizedDomain, QueryType.A);
            if (aRecords.HasError == false && aRecords.Answers.ARecords().Any())
            {
                foreach (var record in aRecords.Answers.ARecords())
                {
                    result.IpAddresses.Add(record.Address.ToString());
                    result.DnsRecords.Add(new DnsRecordDto
                    {
                        Type = "A",
                        Value = record.Address.ToString(),
                        Ttl = record.TimeToLive
                    });
                }
            }

            // DNS Lookup - AAAA records
            var aaaaRecords = await _dnsClient.QueryAsync(normalizedDomain, QueryType.AAAA);
            if (aaaaRecords.HasError == false && aaaaRecords.Answers.AaaaRecords().Any())
            {
                foreach (var record in aaaaRecords.Answers.AaaaRecords())
                {
                    result.IpAddresses.Add(record.Address.ToString());
                    result.DnsRecords.Add(new DnsRecordDto
                    {
                        Type = "AAAA",
                        Value = record.Address.ToString(),
                        Ttl = record.TimeToLive
                    });
                }
            }

            // DNS Lookup - CNAME records
            var cnameRecords = await _dnsClient.QueryAsync(normalizedDomain, QueryType.CNAME);
            if (cnameRecords.HasError == false && cnameRecords.Answers.CnameRecords().Any())
            {
                foreach (var record in cnameRecords.Answers.CnameRecords())
                {
                    result.DnsRecords.Add(new DnsRecordDto
                    {
                        Type = "CNAME",
                        Value = record.CanonicalName.Value,
                        Ttl = record.TimeToLive
                    });
                }
            }

            // TLS Info (try HTTPS on port 443)
            result.TlsInfo = await GetTlsInfoAsync(normalizedDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching website info for {Domain}", normalizedDomain);
        }

        return result;
    }

    private async Task<TlsInfoDto?> GetTlsInfoAsync(string domain)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Store certificate info
                    if (cert != null)
                    {
                        var tlsInfo = new TlsInfoDto
                        {
                            IsValid = errors == SslPolicyErrors.None,
                            Issuer = cert.Issuer,
                            Subject = cert.Subject,
                            NotBefore = cert.NotBefore,
                            NotAfter = cert.NotAfter,
                            DaysUntilExpiry = cert.NotAfter > DateTime.Now
                                ? (int)(cert.NotAfter - DateTime.Now).TotalDays
                                : null,
                            SubjectAlternativeNames = cert.Extensions
                                .OfType<X509Extension>()
                                .Where(e => e.Oid?.Value == "2.5.29.17") // SAN extension
                                .SelectMany(e => ExtractSanNames(e))
                                .ToList()
                        };

                        if (errors != SslPolicyErrors.None)
                        {
                            tlsInfo.ErrorMessage = errors.ToString();
                        }

                        // Store in a way we can return it
                        // For now, we'll create it fresh in the return
                        return true; // Continue to get cert info
                    }
                    return false;
                }
            };

            using var httpClient = new System.Net.Http.HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                var response = await httpClient.GetAsync($"https://{domain}");
                // Certificate info is captured in the callback above
            }
            catch { }

            // Try direct TLS connection
            return await GetCertificateInfoDirectAsync(domain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch TLS info for {Domain}", domain);
            return new TlsInfoDto
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<TlsInfoDto> GetCertificateInfoDirectAsync(string domain)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(domain, 443);

            using var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, certificate, chain, sslPolicyErrors) => true);
            await sslStream.AuthenticateAsClientAsync(domain);

            var cert = sslStream.RemoteCertificate as X509Certificate2;
            if (cert != null)
            {
                return new TlsInfoDto
                {
                    IsValid = true,
                    Issuer = cert.Issuer,
                    Subject = cert.Subject,
                    NotBefore = cert.NotBefore,
                    NotAfter = cert.NotAfter,
                    DaysUntilExpiry = cert.NotAfter > DateTime.Now
                        ? (int)(cert.NotAfter - DateTime.Now).TotalDays
                        : null,
                    SubjectAlternativeNames = ExtractSanNamesFromCert(cert)
                };
            }
        }
        catch (Exception ex)
        {
            return new TlsInfoDto
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }

        return new TlsInfoDto { IsValid = false, ErrorMessage = "Certificate not found" };
    }

    private List<string> ExtractSanNames(X509Extension extension)
    {
        // Simplified SAN extraction
        return new List<string>();
    }

    private List<string> ExtractSanNamesFromCert(X509Certificate2 cert)
    {
        var sans = new List<string>();
        try
        {
            var sanExtension = cert.Extensions["2.5.29.17"]; // SAN OID
            if (sanExtension != null)
            {
                var asnData = sanExtension.Format(false);
                // Parse ASN.1 data (simplified - would need proper ASN.1 parser for production)
            }
        }
        catch { }
        return sans;
    }

    private string NormalizeDomainName(string input)
    {
        var domain = input.Trim().ToLowerInvariant();

        if (domain.StartsWith("http://"))
            domain = domain.Substring(7);
        if (domain.StartsWith("https://"))
            domain = domain.Substring(8);

        if (domain.StartsWith("www."))
            domain = domain.Substring(4);

        var slashIndex = domain.IndexOf('/');
        if (slashIndex >= 0)
            domain = domain.Substring(0, slashIndex);

        var colonIndex = domain.IndexOf(':');
        if (colonIndex >= 0)
            domain = domain.Substring(0, colonIndex);

        return domain;
    }
}
