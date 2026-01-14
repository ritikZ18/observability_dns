// TODO: Implement API startup and configuration
//
// Example structure:
namespace ObservabilityDns.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // TODO: Implement API startup
        // var builder = WebApplication.CreateBuilder(args);
        // ... (see comments below)
    }
}

//
// var builder = WebApplication.CreateBuilder(args);
//
// // Add services
// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
//
// // Add health checks
// builder.Services.AddHealthChecks()
//     .AddCheck("self", () => HealthCheckResult.Healthy())
//     .AddDbContextCheck<ObservabilityDnsDbContext>("database");
//
// // Add OpenTelemetry
// builder.Services.AddOpenTelemetry()
//     .WithTracing(builder => builder
//         .AddAspNetCoreInstrumentation()
//         .AddHttpClientInstrumentation()
//         .AddOtlpExporter(options =>
//         {
//             options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
//         }))
//     .WithMetrics(builder => builder
//         .AddAspNetCoreInstrumentation()
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
// var app = builder.Build();
//
// // Configure pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
//
// app.UseHttpsRedirection();
// app.UseAuthorization();
// app.MapControllers();
//
// // Health check endpoints
// app.MapHealthChecks("/healthz", new HealthCheckOptions
// {
//     Predicate = _ => true,
//     ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
// });
//
// app.MapHealthChecks("/readyz", new HealthCheckOptions
// {
//     Predicate = check => check.Tags.Contains("ready"),
//     ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
// });
//
// app.Run();
