using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Domain.Entities;
using ObservabilityDns.Worker.Probers;
using ObservabilityDns.Worker.Scheduler;

namespace ObservabilityDns.Worker;

/// <summary>
/// Manual worker that continuously runs probe jobs for all enabled domains.
/// Runs until the application is stopped (Ctrl+C or Docker stop).
/// </summary>
public class ManualWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ManualWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public ManualWorker(
        IServiceProvider serviceProvider,
        ILogger<ManualWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Check every 30 seconds by default, can be configured via environment variable
        var intervalSeconds = Environment.GetEnvironmentVariable("MANUAL_WORKER_INTERVAL") ?? "30";
        _checkInterval = TimeSpan.FromSeconds(int.Parse(intervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Manual Worker starting. Check interval: {Interval}", _checkInterval);
        _logger.LogInformation("Press Ctrl+C or stop container to exit");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEnabledDomainsAsync(stoppingToken);
                
                _logger.LogDebug("Waiting {Interval} until next check...", _checkInterval);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Manual Worker stopping (cancellation requested)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in manual worker loop");
                // Continue after error, wait a bit before retrying
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Manual Worker stopped");
    }

    private async Task ProcessEnabledDomainsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ObservabilityDnsDbContext>();

        // Get all enabled domains with their enabled checks
        var domainsWithChecks = await dbContext.Domains
            .Include(d => d.Checks)
            .Where(d => d.Enabled)
            .Select(d => new
            {
                Domain = d,
                Checks = d.Checks.Where(c => c.Enabled).ToList()
            })
            .ToListAsync(cancellationToken);

        if (domainsWithChecks.Count == 0)
        {
            _logger.LogDebug("No enabled domains found. Waiting for domains to be added...");
            return;
        }

        _logger.LogInformation("Processing {Count} domain(s) with enabled checks", domainsWithChecks.Count);

        var jobExecutor = new ProbeJobExecutor(
            scope.ServiceProvider,
            scope.ServiceProvider.GetRequiredService<ILogger<ProbeJobExecutor>>());

        // Process each domain's checks
        foreach (var domainWithChecks in domainsWithChecks)
        {
            var domain = domainWithChecks.Domain;
            
            foreach (var check in domainWithChecks.Checks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation(
                        "Executing {CheckType} probe for domain {DomainName} (ID: {DomainId})",
                        check.CheckType, domain.Name, domain.Id);

                    await jobExecutor.ExecuteProbeAsync(
                        domain.Id,
                        domain.Name,
                        check.Id,
                        check.CheckType,
                        cancellationToken);

                    _logger.LogInformation(
                        "Completed {CheckType} probe for {DomainName}",
                        check.CheckType, domain.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to execute {CheckType} probe for domain {DomainName}",
                        check.CheckType, domain.Name);
                }

                // Small delay between probes to avoid overwhelming the system
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        _logger.LogInformation("Completed processing all enabled domains");
    }
}

/// <summary>
/// Helper class to execute probe jobs (extracted from ProbeJob for reuse)
/// </summary>
public class ProbeJobExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProbeJobExecutor> _logger;

    public ProbeJobExecutor(
        IServiceProvider serviceProvider,
        ILogger<ProbeJobExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteProbeAsync(
        Guid domainId,
        string domainName,
        Guid checkId,
        string checkType,
        CancellationToken cancellationToken)
    {
        var dbContext = _serviceProvider.GetRequiredService<ObservabilityDnsDbContext>();

        ProbeResult? result = null;
        var startedAt = DateTime.UtcNow;

        // Run appropriate probe
        switch (checkType.ToUpperInvariant())
        {
            case "DNS":
                var dnsRunner = _serviceProvider.GetRequiredService<IDnsProbeRunner>();
                var dnsResult = await dnsRunner.ResolveAsync(domainName, cancellationToken);
                result = dnsResult;
                break;

            case "TLS":
                var tlsRunner = _serviceProvider.GetRequiredService<ITlsProbeRunner>();
                var tlsResult = await tlsRunner.CheckCertificateAsync(domainName, 443, cancellationToken);
                result = tlsResult;
                break;

            case "HTTP":
                var httpRunner = _serviceProvider.GetRequiredService<IHttpProbeRunner>();
                var httpUrl = domainName.StartsWith("http") ? domainName : $"https://{domainName}";
                var httpResult = await httpRunner.CheckUrlAsync(httpUrl, cancellationToken);
                result = httpResult;
                break;

            default:
                _logger.LogWarning("Unknown check type: {CheckType}", checkType);
                return;
        }

        if (result == null)
        {
            return;
        }

        var completedAt = DateTime.UtcNow;

        // Save probe run
        var probeRun = new ProbeRun
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
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Probe completed for {Domain} ({CheckType}): Success={Success}, Duration={Duration}ms",
            domainName, checkType, result.Success, result.Duration.TotalMilliseconds);
    }
}
