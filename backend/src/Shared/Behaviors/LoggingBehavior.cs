using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior that logs request/response details with structured logging.
/// Logs handler name, execution time, and warns on slow requests (>500ms).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[MediatR] Handling {RequestName} ({RequestId})",
            requestName, requestId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "[MediatR] ⚠️ Slow handler {RequestName} ({RequestId}) completed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    requestName, requestId, elapsedMs, SlowRequestThresholdMs);
            }
            else
            {
                _logger.LogInformation(
                    "[MediatR] ✅ {RequestName} ({RequestId}) completed in {ElapsedMs}ms",
                    requestName, requestId, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[MediatR] ❌ {RequestName} ({RequestId}) failed after {ElapsedMs}ms — {ErrorMessage}",
                requestName, requestId, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}
