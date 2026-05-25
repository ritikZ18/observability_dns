using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObservabilityDns.Domain.DbContext;
using ObservabilityDns.Worker.Scheduler;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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
var serviceVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(
            serviceName: "ObservabilityDns.Worker",
            serviceVersion: serviceVersion,
            serviceInstanceId: Environment.MachineName));
    options.AddOtlpExporter(exporterOptions =>
    {
        exporterOptions.Endpoint = new Uri(otelEndpoint);
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "ObservabilityDns.Worker",
        serviceVersion: serviceVersion,
        serviceInstanceId: Environment.MachineName))
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
// Support both DATABASE_URL (Fly.io) and ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ObservabilityDnsDbContext>(options =>
    options.UseNpgsql(connectionString));

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
