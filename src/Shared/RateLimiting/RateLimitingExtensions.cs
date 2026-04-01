using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace IMS.Modular.Shared.RateLimiting;

/// <summary>
/// Extension methods for configuring rate limiting policies.
/// US-009: Rate Limiting — protects auth endpoints (per-IP) and applies a global fallback.
///
/// Policies:
///   • AuthPolicy  — fixed window per-IP for /api/auth/login and /api/auth/register
///   • GlobalPolicy — fixed window global fallback for all other endpoints
///
/// Returns 429 Too Many Requests with Retry-After header when limits are exceeded.
/// All settings are configurable via appsettings.json → RateLimiting section.
/// </summary>
public static class RateLimitingExtensions
{
    // ── Policy name constants ───────────────────────────────────────────────
    public static class Policies
    {
        /// <summary>Strict per-IP limiter for authentication endpoints (login, register).</summary>
        public const string Auth = "AuthPolicy";

        /// <summary>Global fallback limiter applied to all endpoints.</summary>
        public const string Global = "GlobalPolicy";
    }

    // ── Default values (used when appsettings.json section is missing) ──────
    private static class Defaults
    {
        // Auth policy defaults
        public const int AuthPermitLimit = 5;
        public const int AuthWindowSeconds = 60;
        public const int AuthQueueLimit = 0;

        // Global policy defaults
        public const int GlobalPermitLimit = 100;
        public const int GlobalWindowSeconds = 60;
        public const int GlobalQueueLimit = 10;
    }

    /// <summary>
    /// Registers ASP.NET Core Rate Limiting services and defines named policies.
    /// Call in <c>builder.Services.AddImsRateLimiting(builder.Configuration)</c>.
    /// </summary>
    public static IServiceCollection AddImsRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // ── Global rejection handler ────────────────────────────────────
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue
                    : TimeSpan.FromSeconds(60);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                context.HttpContext.Response.ContentType = "application/json";

                var body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    RetryAfterSeconds = (int)retryAfter.TotalSeconds
                });

                await context.HttpContext.Response.WriteAsync(body, cancellationToken);
            };

            // ── Auth Policy (per-IP, fixed window) ─────────────────────────
            var authPermitLimit = section.GetValue("Auth:PermitLimit", Defaults.AuthPermitLimit);
            var authWindowSeconds = section.GetValue("Auth:WindowSeconds", Defaults.AuthWindowSeconds);
            var authQueueLimit = section.GetValue("Auth:QueueLimit", Defaults.AuthQueueLimit);

            options.AddPolicy(Policies.Auth, httpContext =>
            {
                var clientIp = GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = authQueueLimit,
                        AutoReplenishment = true
                    });
            });

            // ── Global Policy (per-IP, fixed window — more permissive) ─────
            var globalPermitLimit = section.GetValue("Global:PermitLimit", Defaults.GlobalPermitLimit);
            var globalWindowSeconds = section.GetValue("Global:WindowSeconds", Defaults.GlobalWindowSeconds);
            var globalQueueLimit = section.GetValue("Global:QueueLimit", Defaults.GlobalQueueLimit);

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIp = GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(globalWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = globalQueueLimit,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP context.
    /// Respects X-Forwarded-For header (first entry) when behind a reverse proxy,
    /// falling back to the direct connection IP.
    /// </summary>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check X-Forwarded-For first (reverse proxy scenario)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs — take the first (original client)
            var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (!string.IsNullOrWhiteSpace(firstIp))
                return firstIp;
        }

        // Direct connection IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
