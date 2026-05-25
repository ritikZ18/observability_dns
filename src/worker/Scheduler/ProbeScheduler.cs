using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Domain.Entities;
using Quartz;
using Quartz.Impl.Matchers;
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
            .ToListAsync(cancellationToken);

        var desired = new Dictionary<JobKey, (ObservabilityDns.Domain.Entities.Domain Domain, ObservabilityDns.Domain.Entities.Check Check)>();
        foreach (var domain in domains.Where(d => d.Enabled))
        {
            foreach (var check in domain.Checks.Where(c => c.Enabled))
            {
                var jobKey = new JobKey($"probe-{domain.Id}-{check.CheckType}", "probes");
                desired[jobKey] = (domain, check);
            }
        }

        // Ensure all desired jobs exist and have the right interval
        foreach (var (jobKey, pair) in desired)
        {
            var (domain, check) = pair;
            var triggerKey = new TriggerKey($"trigger-{domain.Id}-{check.CheckType}", "probes");

            var desiredInterval = TimeSpan.FromMinutes(domain.IntervalMinutes);

            if (!await _scheduler!.CheckExists(jobKey, cancellationToken))
            {
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

                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithInterval(desiredInterval)
                        .RepeatForever())
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                _logger.LogInformation("Scheduled probe job for {Domain} ({CheckType}) every {Interval} minutes",
                    domain.Name, check.CheckType, domain.IntervalMinutes);
                continue;
            }

            var existingTrigger = await _scheduler.GetTrigger(triggerKey, cancellationToken);
            if (existingTrigger is ISimpleTrigger simpleTrigger)
            {
                if (simpleTrigger.RepeatInterval != desiredInterval)
                {
                    var updatedTrigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithInterval(desiredInterval)
                            .RepeatForever())
                        .Build();

                    await _scheduler.RescheduleJob(triggerKey, updatedTrigger, cancellationToken);
                    _logger.LogInformation("Rescheduled probe job for {Domain} ({CheckType}) to {Interval} minutes",
                        domain.Name, check.CheckType, domain.IntervalMinutes);
                }
            }
            else if (existingTrigger == null)
            {
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .ForJob(jobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithInterval(desiredInterval)
                        .RepeatForever())
                    .Build();

                await _scheduler.ScheduleJob(trigger, cancellationToken);
                _logger.LogInformation("Added missing trigger for {Domain} ({CheckType})", domain.Name, check.CheckType);
            }
        }

        // Clean up jobs for disabled/removed domains/checks
        var existingJobs = await _scheduler!.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("probes"), cancellationToken);
        foreach (var jobKey in existingJobs)
        {
            if (!desired.ContainsKey(jobKey))
            {
                await _scheduler.DeleteJob(jobKey, cancellationToken);
                _logger.LogInformation("Deleted obsolete probe job {JobKey}", jobKey.Name);
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
