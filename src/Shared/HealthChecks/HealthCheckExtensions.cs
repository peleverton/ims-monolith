using System.Text.Json;
using System.Text.Json.Serialization;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IMS.Modular.Shared.HealthChecks;

/// <summary>
/// Extension methods for registering health checks and mapping the /health endpoint.
/// US-007: Health Checks — DB, Memory, Disk, Cache.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers all health checks: DB contexts, memory pressure, disk space, Redis (conditional).
    /// </summary>
    public static IServiceCollection AddImsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from appsettings.json
        services.Configure<MemoryHealthCheckOptions>(
            configuration.GetSection("HealthChecks:Memory"));
        services.Configure<DiskSpaceHealthCheckOptions>(
            configuration.GetSection("HealthChecks:DiskSpace"));

        var healthChecksBuilder = services.AddHealthChecks();

        // DB connectivity — Auth module
        healthChecksBuilder.AddDbContextCheck<AuthDbContext>(
            name: "database-auth",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "auth", "ready"]);

        // DB connectivity — Issues module
        healthChecksBuilder.AddDbContextCheck<IssuesDbContext>(
            name: "database-issues",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "issues", "ready"]);

        // Memory pressure
        healthChecksBuilder.AddCheck<MemoryHealthCheck>(
            name: "memory",
            failureStatus: HealthStatus.Degraded,
            tags: ["memory", "ready"]);

        // Disk space
        healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
            name: "disk-space",
            failureStatus: HealthStatus.Degraded,
            tags: ["disk", "ready"]);

        // Redis connectivity — conditional on configuration
        // Will be activated when Redis is configured (US-008)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecksBuilder.AddCheck<CacheHealthCheck>(
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"]);
        }

        return services;
    }

    /// <summary>
    /// Maps health check endpoints:
    ///   /health       — overall system health with per-check details (JSON)
    ///   /health/live  — liveness probe (lightweight, always returns 200 if process is running)
    ///   /health/ready — readiness probe (checks all dependencies tagged "ready")
    /// </summary>
    public static WebApplication MapImsHealthChecks(this WebApplication app)
    {
        // Detailed health check endpoint — JSON response with per-check details
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthResponse,
            AllowCachingResponses = false
        })
        .AllowAnonymous()
        .WithTags("System");

        // Liveness probe — lightweight, no dependency checks
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks — just confirms process is alive
            AllowCachingResponses = false
        })
        .AllowAnonymous()
        .WithTags("System");

        // Readiness probe — checks all dependencies tagged "ready"
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteDetailedHealthResponse,
            AllowCachingResponses = false
        })
        .AllowAnonymous()
        .WithTags("System");

        return app;
    }

    /// <summary>
    /// Writes a detailed JSON response with overall status and per-check details.
    /// </summary>
    private static async Task WriteDetailedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds.ToString("F2") + "ms",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds.ToString("F2") + "ms",
                tags = entry.Value.Tags,
                data = entry.Value.Data.Count > 0
                    ? entry.Value.Data.ToDictionary(d => d.Key, d => d.Value)
                    : null,
                exception = entry.Value.Exception?.Message
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, options));
    }
}
