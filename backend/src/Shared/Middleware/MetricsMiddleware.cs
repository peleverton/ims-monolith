using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Records HTTP request count and latency histograms using System.Diagnostics.Metrics.
/// Prometheus-compatible when paired with OpenTelemetry Metrics exporter.
/// </summary>
public sealed class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Meter Meter = new("IMS.Modular.Http", "1.0.0");

    private static readonly Counter<long> RequestCounter =
        Meter.CreateCounter<long>("http_requests_total", "requests",
            "Total number of HTTP requests");

    private static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("http_request_duration_ms", "ms",
            "HTTP request duration in milliseconds");

    public MetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var tags = new TagList
            {
                { "method", context.Request.Method },
                { "path", GetNormalizedPath(context) },
                { "status_code", context.Response.StatusCode.ToString() }
            };

            RequestCounter.Add(1, tags);
            RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        }
    }

    /// <summary>
    /// Normalizes the path to avoid high-cardinality metrics.
    /// Replaces GUIDs and numeric IDs with placeholders.
    /// </summary>
    private static string GetNormalizedPath(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Replace GUIDs: /api/issues/550e8400-... → /api/issues/{id}
        path = System.Text.RegularExpressions.Regex.Replace(
            path, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "{id}");

        // Replace numeric IDs: /api/items/123 → /api/items/{id}
        path = System.Text.RegularExpressions.Regex.Replace(
            path, @"/\d+", "/{id}");

        return path;
    }
}
