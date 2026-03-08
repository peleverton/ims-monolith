using System.Diagnostics;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Logs a warning for HTTP requests that exceed the configured threshold (default 500ms).
/// Helps identify slow endpoints that may need optimization.
/// </summary>
public sealed class PerformanceTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTimingMiddleware> _logger;
    private readonly int _thresholdMs;

    public PerformanceTimingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTimingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _thresholdMs = configuration.GetValue("Performance:SlowRequestThresholdMs", 500);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > _thresholdMs)
        {
            _logger.LogWarning(
                "⚠️ Slow request: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs,
                _thresholdMs);
        }
    }
}
