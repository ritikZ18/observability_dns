// TODO: Implement Worker Service startup and configuration
//
// Example structure:
namespace ObservabilityDns.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        // TODO: Implement Worker Service startup
        // var builder = Host.CreateApplicationBuilder(args);
        // ... (see comments below)
    }
}

//
// var builder = Host.CreateApplicationBuilder(args);
//
// // Add services
// builder.Services.AddHostedService<Worker>();
//
// // Add Quartz scheduler
// builder.Services.AddQuartz(q =>
// {
//     q.UseMicrosoftDependencyInjection();
//     // Configure jobs for DNS, TLS, HTTP probes
// });
// builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
//
// // Add OpenTelemetry
// builder.Services.AddOpenTelemetry()
//     .WithTracing(builder => builder
//         .AddHttpClientInstrumentation()
//         .AddOtlpExporter(options =>
//         {
//             options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
//         }))
//     .WithMetrics(builder => builder
//         .AddHttpClientInstrumentation()
//         .AddOtlpExporter(options =>
//         {
//             options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
//         }));
//
// // Add DbContext
// builder.Services.AddDbContext<ObservabilityDnsDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//
// // Register probe runners
// builder.Services.AddScoped<IDnsProbeRunner, DnsProbeRunner>();
// builder.Services.AddScoped<ITlsProbeRunner, TlsProbeRunner>();
// builder.Services.AddScoped<IHttpProbeRunner, HttpProbeRunner>();
//
// // Health check logging (write to database or log file)
// var host = builder.Build();
//
// // Log worker startup
// var logger = host.Services.GetRequiredService<ILogger<Program>>();
// logger.LogInformation("Observability DNS Worker starting...");
//
// host.Run();
