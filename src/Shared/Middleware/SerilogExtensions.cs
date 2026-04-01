using IMS.Modular.Shared.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Extension methods for configuring Serilog structured logging.
/// US-006: Structured Logging — Serilog + JSON + Correlation ID Enrichment.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider with:
    /// - Structured JSON output (console + file)
    /// - Rolling file logs (daily, 100MB limit, 31-day retention)
    /// - Correlation ID enrichment on every log entry
    /// - Request context enrichment (path, method, userId)
    /// - Sensitive data exclusion
    /// - Configurable verbosity via appsettings.json
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            var correlationIdAccessor = services.GetService<ICorrelationIdAccessor>();
            var httpContextAccessor = services.GetService<IHttpContextAccessor>();

            loggerConfiguration
                // Read minimum level + overrides from appsettings.json → "Serilog" section
                .ReadFrom.Configuration(context.Configuration)

                // Enrich every log entry with context
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "IMS.Modular")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("MachineName", System.Environment.MachineName)

                // Correlation ID enrichment (US-006 core requirement)
                .Enrich.With(new CorrelationIdEnricher(
                    correlationIdAccessor ?? new CorrelationIdAccessor(
                        services.GetRequiredService<IHttpContextAccessor>())))

                // Request context enrichment
                .Enrich.With(new RequestContextEnricher(
                    httpContextAccessor ?? services.GetRequiredService<IHttpContextAccessor>()))

                // Sensitive data filtering
                .Destructure.With<SensitiveDataDestructuringPolicy>()

                // Console sink — JSON structured output
                .WriteTo.Console(new RenderedCompactJsonFormatter())

                // File sink — rolling daily, 100MB max per file, 31-day retention
                .WriteTo.File(
                    formatter: new RenderedCompactJsonFormatter(),
                    path: "logs/ims-modular-.log",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                    retainedFileCountLimit: 31,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
        });

        return builder;
    }

    /// <summary>
    /// Adds Serilog request logging middleware with configurable verbosity.
    /// Logs request/response details (method, path, status code, elapsed time).
    /// Sensitive paths (auth endpoints) get reduced log level.
    /// </summary>
    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            // Enrich diagnostic context with additional data
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown");

                var correlationId = httpContext.Items["CorrelationId"]?.ToString();
                if (correlationId is not null)
                    diagnosticContext.Set("CorrelationId", correlationId);

                var userId = httpContext.User?.FindFirst("sub")?.Value
                          ?? httpContext.User?.FindFirst("nameid")?.Value;
                if (userId is not null)
                    diagnosticContext.Set("UserId", userId);
            };

            // Reduce noise: health checks and system endpoints at Debug level
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex is not null)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                // Health checks and ping at Debug level
                var path = httpContext.Request.Path.Value ?? "";
                if (path.StartsWith("/api/ping", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/status", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                    return LogEventLevel.Debug;

                // Slow requests get Warning
                if (elapsed > 500)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };
        });

        return app;
    }
}
