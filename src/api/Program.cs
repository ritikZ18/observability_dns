using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ObservabilityDns.Domain.DbContext;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DNS & TLS Observatory API", Version = "v1" });
});

// Add CORS for UI
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is healthy"))
    .AddDbContextCheck<ObservabilityDnsDbContext>("database", tags: new[] { "ready" });

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
            serviceName: "ObservabilityDns.Api",
            serviceVersion: serviceVersion,
            serviceInstanceId: Environment.MachineName));
    options.AddOtlpExporter(exporterOptions =>
    {
        exporterOptions.Endpoint = new Uri(otelEndpoint);
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "ObservabilityDns.Api",
        serviceVersion: serviceVersion,
        serviceInstanceId: Environment.MachineName))
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .AddAspNetCoreInstrumentation()
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

// Register services
builder.Services.AddScoped<ObservabilityDns.Api.Services.DomainService>();
builder.Services.AddScoped<ObservabilityDns.Api.Services.GroupService>();
builder.Services.AddScoped<ObservabilityDns.Api.Services.ProbeRunService>();
builder.Services.AddScoped<ObservabilityDns.Api.Services.WebsiteInfoService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

static Task WriteHealthResponse(HttpContext context, HealthReport report, bool includeDuration)
{
    context.Response.ContentType = "application/json";
    var result = System.Text.Json.JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            exception = e.Value.Exception?.Message,
            duration = includeDuration ? e.Value.Duration.ToString() : null
        })
    });
    return context.Response.WriteAsync(result);
}

// Health check endpoints
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
    ResponseWriter = (context, report) => WriteHealthResponse(context, report, includeDuration: true)
});

app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
    ResponseWriter = (context, report) => WriteHealthResponse(context, report, includeDuration: false)
});

app.Run();
