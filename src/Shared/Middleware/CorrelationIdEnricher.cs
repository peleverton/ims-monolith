using Serilog.Core;
using Serilog.Events;
using IMS.Modular.Shared.Abstractions;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Serilog enricher that adds the current request's Correlation ID to every log entry.
/// Works via ICorrelationIdAccessor which reads from HttpContext.Items.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    public const string PropertyName = "CorrelationId";

    public CorrelationIdEnricher(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _correlationIdAccessor.CorrelationId ?? "no-correlation-id";
        var property = propertyFactory.CreateProperty(PropertyName, correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }
}

/// <summary>
/// Serilog enricher that adds Module, Handler, UserId properties from the active scope.
/// Reads from HttpContext.Items where middleware stores context values.
/// </summary>
public sealed class RequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        // Add request path
        var path = httpContext.Request.Path.Value;
        if (path is not null)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", path));

        // Add HTTP method
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("HttpMethod", httpContext.Request.Method));

        // Add UserId from claims (if authenticated)
        var userId = httpContext.User?.FindFirst("sub")?.Value
                  ?? httpContext.User?.FindFirst("nameid")?.Value;
        if (userId is not null)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
    }
}
