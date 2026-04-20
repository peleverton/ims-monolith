using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace IMS.Modular.Shared.Observability;

/// <summary>
/// US-010 / US-027: OpenTelemetry — distributed tracing, custom metrics, Prometheus, Jaeger.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>Application-level ActivitySource for custom tracing spans.</summary>
    public static readonly ActivitySource ActivitySource = new("IMS.Modular", "1.0.0");

    /// <summary>Application-level Meter for custom metrics.</summary>
    public static readonly Meter AppMeter = new("IMS.Modular.App", "1.0.0");

    // US-027: Custom instruments
    private static readonly Counter<long> _domainEventsPublished =
        AppMeter.CreateCounter<long>("ims.domain_events.published", "events",
            "Total domain events published after SaveChanges");

    private static readonly Histogram<double> _outboxProcessingDuration =
        AppMeter.CreateHistogram<double>("ims.outbox.processing_duration_ms", "ms",
            "Duration of outbox processing cycles");

    /// <summary>Increments the domain events published counter.</summary>
    public static void RecordDomainEventPublished(string eventType)
        => _domainEventsPublished.Add(1, new KeyValuePair<string, object?>("event.type", eventType));

    /// <summary>Records outbox processing cycle duration in milliseconds.</summary>
    public static void RecordOutboxProcessingDuration(double milliseconds)
        => _outboxProcessingDuration.Record(milliseconds);

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
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? "";
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
                        };
                        options.RecordException = true;
                        // US-027: propagate Correlation-ID into trace
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            if (request.Headers.TryGetValue("X-Correlation-Id", out var corrId))
                                activity.SetTag("ims.correlation_id", corrId.ToString());
                        };
                    })
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddSource(ActivitySource.Name);

                // US-043: OTLP exporter (Jaeger, Grafana Tempo, etc.)
                var otlpEndpoint = section.GetValue<string>("OtlpEndpoint");
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("IMS.Modular.Http")
                    .AddMeter(AppMeter.Name)
                    .AddPrometheusExporter();
            });

        return services;
    }

    public static WebApplication MapImsMetrics(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint("/metrics")
            .AllowAnonymous()
            .ExcludeFromDescription();
        return app;
    }

    /// <summary>Creates a custom activity (span) for application-level tracing.</summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => ActivitySource.StartActivity(name, kind);
}
