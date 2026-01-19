using DnsClient;
using DnsClient.Protocol;
using ObservabilityDns.Worker.Probers;
using System.Diagnostics;
using System.Text.Json;

namespace ObservabilityDns.Worker.Probers.Dns;

public class DnsProbeRunner : IDnsProbeRunner
{
    private readonly LookupClient _dnsClient;
    private readonly ILogger<DnsProbeRunner> _logger;

    public DnsProbeRunner(ILogger<DnsProbeRunner> logger)
    {
        _logger = logger;
        _dnsClient = new LookupClient();
    }

    public async Task<ProbeResult> RunProbeAsync(string target, CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(target, cancellationToken);
    }

    public async Task<DnsProbeResult> ResolveAsync(string domain, CancellationToken cancellationToken = default)
    {
        var result = new DnsProbeResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Resolve A records
            var aQuery = await _dnsClient.QueryAsync(domain, QueryType.A);
            if (aQuery.HasError)
            {
                result.Success = false;
                result.ErrorCode = aQuery.ErrorMessage;
                result.ErrorMessage = $"DNS query failed: {aQuery.ErrorMessage}";
                return result;
            }

            var aRecords = aQuery.Answers.ARecords().ToList();
            foreach (var record in aRecords)
            {
                result.IpAddresses.Add(record.Address.ToString());
                result.Records.Add(new DnsRecord
                {
                    Type = "A",
                    Value = record.Address.ToString(),
                    Ttl = record.TimeToLive
                });
            }

            // Resolve AAAA records
            var aaaaQuery = await _dnsClient.QueryAsync(domain, QueryType.AAAA);
            if (!aaaaQuery.HasError)
            {
                var aaaaRecords = aaaaQuery.Answers.AaaaRecords().ToList();
                foreach (var record in aaaaRecords)
                {
                    result.IpAddresses.Add(record.Address.ToString());
                    result.Records.Add(new DnsRecord
                    {
                        Type = "AAAA",
                        Value = record.Address.ToString(),
                        Ttl = record.TimeToLive
                    });
                }
            }

            // Resolve CNAME records
            var cnameQuery = await _dnsClient.QueryAsync(domain, QueryType.CNAME);
            if (!cnameQuery.HasError)
            {
                var cnameRecords = cnameQuery.Answers.CnameRecords().ToList();
                foreach (var record in cnameRecords)
                {
                    result.Records.Add(new DnsRecord
                    {
                        Type = "CNAME",
                        Value = record.CanonicalName.Value,
                        Ttl = record.TimeToLive
                    });
                }
            }

            result.Success = result.Records.Any();
            if (!result.Success)
            {
                result.ErrorCode = "NXDOMAIN";
                result.ErrorMessage = "No DNS records found";
            }

            // Create JSON snapshot
            result.RecordsSnapshot = JsonDocument.Parse(JsonSerializer.Serialize(result.Records));
        }
        catch (DnsResponseException ex)
        {
            result.Success = false;
            result.ErrorCode = ex.Code.ToString();
            result.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "DNS resolution failed for {Domain}", domain);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorCode = "UNKNOWN";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error resolving DNS for {Domain}", domain);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }
}
