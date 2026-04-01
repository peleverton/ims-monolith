using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace IMS.Modular.Shared.Observability;

/// <summary>
/// Extension methods for registering OpenTelemetry services (traces + metrics + Prometheus exporter).
/// US-010: Observability — provides distributed tracing and Prometheus-compatible metrics endpoint.
///
/// Traces:
///   • ASP.NET Core instrumentation (HTTP requests)
///   • HttpClient instrumentation (outbound calls)
///   • EF Core instrumentation (database queries)
///   • Custom ActivitySource: "IMS.Modular" for application-level spans
///
/// Metrics:
///   • ASP.NET Core metrics (request count, duration, active connections)
///   • Runtime metrics (.NET GC, thread pool, etc.)
///   • Custom Meter: "IMS.Modular.Http" (from MetricsMiddleware)
///   • Prometheus exporter: /metrics endpoint
///
/// All configuration is via appsettings.json → OpenTelemetry section.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>Application-level ActivitySource for custom tracing spans.</summary>
    public static readonly ActivitySource ActivitySource = new("IMS.Modular", "1.0.0");

    /// <summary>Application-level Meter for custom metrics beyond the MetricsMiddleware.</summary>
    public static readonly Meter AppMeter = new("IMS.Modular.App", "1.0.0");

    /// <summary>
    /// Registers OpenTelemetry tracing and metrics with Prometheus exporter.
    /// Call in <c>builder.Services.AddImsOpenTelemetry(builder.Configuration)</c>.
    /// </summary>
    public static IServiceCollection AddImsOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("OpenTelemetry");
        var serviceName = section.GetValue("ServiceName", "IMS.Modular");
        var serviceVersion = section.GetValue("ServiceVersion", "1.0.0");

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName);

                resource.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out health check and metrics endpoints to reduce noise
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? "";
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
                        };

                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource(ActivitySource.Name); // Register custom ActivitySource

                // OTLP exporter (if endpoint is configured)
                var otlpEndpoint = section.GetValue<string>("OtlpEndpoint");
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("IMS.Modular.Http")   // MetricsMiddleware meter
                    .AddMeter(AppMeter.Name)         // Application-level custom meter
                    .AddPrometheusExporter();         // /metrics endpoint
            });

        return services;
    }

    /// <summary>
    /// Maps the Prometheus scraping endpoint at /metrics.
    /// Call in the middleware pipeline: <c>app.MapImsMetrics()</c>.
    /// </summary>
    public static WebApplication MapImsMetrics(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint("/metrics")
            .AllowAnonymous()
            .ExcludeFromDescription(); // Hide from Swagger

        return app;
    }

    /// <summary>
    /// Creates a custom activity (span) for tracing application-level operations.
    /// Usage: <c>using var activity = OpenTelemetryExtensions.StartActivity("ProcessOrder");</c>
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }
}
