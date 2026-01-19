using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Domain.Entities;
using Quartz;
using System.Text.Json;

namespace ObservabilityDns.Worker.Scheduler;

public class ProbeScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<ProbeScheduler> _logger;
    private IScheduler? _scheduler;

    public ProbeScheduler(
        IServiceProvider serviceProvider,
        ISchedulerFactory schedulerFactory,
        ILogger<ProbeScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
        await _scheduler.Start(stoppingToken);

        _logger.LogInformation("Probe Scheduler started");

        // Schedule initial jobs for all enabled domains
        await ScheduleAllDomainsAsync(stoppingToken);

        // Periodically check for new domains or changes
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            await ScheduleAllDomainsAsync(stoppingToken);
        }
    }

    private async Task ScheduleAllDomainsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ObservabilityDnsDbContext>();

        var domains = await dbContext.Domains
            .Include(d => d.Checks)
            .Where(d => d.Enabled)
            .ToListAsync(cancellationToken);

        foreach (var domain in domains)
        {
            foreach (var check in domain.Checks.Where(c => c.Enabled))
            {
                var jobKey = new JobKey($"probe-{domain.Id}-{check.CheckType}", "probes");
                var triggerKey = new TriggerKey($"trigger-{domain.Id}-{check.CheckType}", "probes");

                // Check if job already exists
                if (await _scheduler!.CheckExists(jobKey, cancellationToken))
                {
                    continue; // Job already scheduled
                }

                // Create job
                var jobData = new JobDataMap
                {
                    ["DomainId"] = domain.Id.ToString(),
                    ["DomainName"] = domain.Name,
                    ["CheckId"] = check.Id.ToString(),
                    ["CheckType"] = check.CheckType
                };

                var job = JobBuilder.Create<ProbeJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData(jobData)
                    .Build();

                // Create trigger based on interval
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithInterval(TimeSpan.FromMinutes(domain.IntervalMinutes))
                        .RepeatForever())
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                _logger.LogInformation("Scheduled probe job for {Domain} ({CheckType}) every {Interval} minutes",
                    domain.Name, check.CheckType, domain.IntervalMinutes);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            _logger.LogInformation("Probe Scheduler stopped");
        }
        await base.StopAsync(cancellationToken);
    }
}
