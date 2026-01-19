using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Worker.Scheduler;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddHostedService<ProbeScheduler>();

// Add Quartz scheduler
builder.Services.AddQuartz(q =>
{
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Add OpenTelemetry
var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
            });
    });

// Add DbContext
builder.Services.AddDbContext<ObservabilityDnsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for HTTP probes
builder.Services.AddHttpClient();

// Register probe runners
builder.Services.AddScoped<ObservabilityDns.Worker.Probers.IDnsProbeRunner, ObservabilityDns.Worker.Probers.Dns.DnsProbeRunner>();
builder.Services.AddScoped<ObservabilityDns.Worker.Probers.ITlsProbeRunner, ObservabilityDns.Worker.Probers.Tls.TlsProbeRunner>();
builder.Services.AddScoped<ObservabilityDns.Worker.Probers.IHttpProbeRunner, ObservabilityDns.Worker.Probers.Http.HttpProbeRunner>();

// Register notification processor
builder.Services.AddHostedService<ObservabilityDns.Worker.Services.NotificationProcessor>();

var host = builder.Build();

// Log worker startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Observability DNS Worker starting...");

host.Run();
