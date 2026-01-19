using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ObservabilityDns.Contracts.Enums;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Worker.Probers;
using Quartz;
using System.Text.Json;

namespace ObservabilityDns.Worker.Scheduler;

[DisallowConcurrentExecution]
public class ProbeJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProbeJob> _logger;

    public ProbeJob(IServiceProvider serviceProvider, ILogger<ProbeJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobData = context.JobDetail.JobDataMap;
        var domainId = Guid.Parse(jobData.GetString("DomainId")!);
        var domainName = jobData.GetString("DomainName")!;
        var checkId = Guid.Parse(jobData.GetString("CheckId")!);
        var checkType = jobData.GetString("CheckType")!;

        _logger.LogInformation("Executing {CheckType} probe for {Domain}", checkType, domainName);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ObservabilityDnsDbContext>();

        try
        {
            ProbeResult? result = null;
            var startedAt = DateTime.UtcNow;

            // Run appropriate probe
            switch (checkType)
            {
                case "DNS":
                    var dnsRunner = scope.ServiceProvider.GetRequiredService<IDnsProbeRunner>();
                    var dnsResult = await dnsRunner.ResolveAsync(domainName);
                    result = dnsResult;
                    break;

                case "TLS":
                    var tlsRunner = scope.ServiceProvider.GetRequiredService<ITlsProbeRunner>();
                    var tlsResult = await tlsRunner.CheckCertificateAsync(domainName);
                    result = tlsResult;
                    break;

                case "HTTP":
                    var httpRunner = scope.ServiceProvider.GetRequiredService<IHttpProbeRunner>();
                    var httpUrl = domainName.StartsWith("http") ? domainName : $"https://{domainName}";
                    var httpResult = await httpRunner.CheckUrlAsync(httpUrl);
                    result = httpResult;
                    break;
            }

            if (result == null)
            {
                _logger.LogWarning("Unknown check type: {CheckType}", checkType);
                return;
            }

            var completedAt = DateTime.UtcNow;

            // Save probe run
            var probeRun = new ObservabilityDns.Domain.Entities.ProbeRun
            {
                DomainId = domainId,
                CheckId = checkId,
                CheckType = checkType,
                Success = result.Success,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                TotalMs = (int)result.Duration.TotalMilliseconds,
                StartedAt = startedAt,
                CompletedAt = completedAt
            };

            // Set type-specific fields
            if (result is DnsProbeResult dnsProbeResult)
            {
                probeRun.DnsMs = (int)result.Duration.TotalMilliseconds;
                probeRun.RecordsSnapshot = dnsProbeResult.RecordsSnapshot;
            }
            else if (result is TlsProbeResult tlsProbeResult)
            {
                probeRun.TlsMs = (int)result.Duration.TotalMilliseconds;
                probeRun.CertificateInfo = tlsProbeResult.CertificateInfo;
            }
            else if (result is HttpProbeResult httpProbeResult)
            {
                probeRun.TtfbMs = httpProbeResult.TtfbMs;
                probeRun.StatusCode = httpProbeResult.StatusCode;
            }

            dbContext.ProbeRuns.Add(probeRun);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Probe completed for {Domain} ({CheckType}): Success={Success}, Duration={Duration}ms",
                domainName, checkType, result.Success, result.Duration.TotalMilliseconds);

            // Check for incidents and create if needed
            await CheckAndCreateIncidentsAsync(dbContext, domainId, checkId, checkType, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing probe job for {Domain} ({CheckType})", domainName, checkType);
        }
    }

    private async Task CheckAndCreateIncidentsAsync(
        ObservabilityDns.Domain.DbContext.ObservabilityDnsDbContext dbContext,
        Guid domainId,
        Guid checkId,
        string checkType,
        ProbeResult result)
    {
        if (result.Success)
        {
            // Resolve any open incidents for this check
            var openIncidents = await dbContext.Incidents
                .Where(i => i.DomainId == domainId && i.CheckType == checkType && i.Status == "OPEN")
                .ToListAsync();

            foreach (var incident in openIncidents)
            {
                incident.Status = "RESOLVED";
                incident.ResolvedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
            }

            if (openIncidents.Any())
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Resolved {Count} incidents for {CheckType} check", openIncidents.Count, checkType);
            }
        }
        else
        {
            // Check if incident already exists
            var existingIncident = await dbContext.Incidents
                .FirstOrDefaultAsync(i => i.DomainId == domainId && i.CheckType == checkType && i.Status == "OPEN");

            if (existingIncident == null)
            {
                // Create new incident
                var incident = new ObservabilityDns.Domain.Entities.Incident
                {
                    DomainId = domainId,
                    CheckType = checkType,
                    Severity = DetermineSeverity(checkType, result),
                    Status = "OPEN",
                    Reason = result.ErrorMessage ?? result.ErrorCode ?? "Probe failed",
                    StartedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Incidents.Add(incident);
                await dbContext.SaveChangesAsync();

                _logger.LogWarning("Created incident for {Domain} ({CheckType}): {Reason}",
                    domainId, checkType, incident.Reason);

                // Create notification in outbox
                // TODO: Implement notification creation based on alert rules
            }
        }
    }

    private string DetermineSeverity(string checkType, ProbeResult result)
    {
        // Determine severity based on check type and error
        if (checkType == "TLS" && result.ErrorCode == "INVALID_CERTIFICATE")
            return "HIGH";
        if (checkType == "HTTP" && result is HttpProbeResult httpResult && httpResult.StatusCode >= 500)
            return "HIGH";
        if (result.ErrorCode == "TIMEOUT" || result.ErrorCode == "CONNECTION_FAILED")
            return "MEDIUM";
        return "LOW";
    }
}
