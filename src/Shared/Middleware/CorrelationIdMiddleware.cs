using IMS.Modular.Shared.Abstractions;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Reads or generates a X-Correlation-Id header for every request.
/// Propagates the correlation ID to the response and makes it available
/// via ICorrelationIdAccessor for structured logging enrichment.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Store in HttpContext.Items for downstream access
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Add to logger scope so all logs in this request include CorrelationId
        using (context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CorrelationId")
            .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
}
