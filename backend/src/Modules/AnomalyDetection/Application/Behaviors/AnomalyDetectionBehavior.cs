using IMS.Modular.Modules.AnomalyDetection.Application.Services;
using IMS.Modular.Modules.Inventory.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace IMS.Modular.Modules.AnomalyDetection.Application.Behaviors;

/// <summary>
/// Decorator Pattern: MediatR Pipeline Behavior que intercepta Commands de movimentação
/// de estoque para avaliar anomalias ANTES de prosseguir com o handler.
///
/// Envolve apenas AdjustStockCommand e CreateStockMovementCommand.
/// Não bloqueia a execução — apenas registra alertas. Bloqueio pode ser
/// habilitado futuramente via flag no threshold config.
/// </summary>
public sealed class AnomalyDetectionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly AnomalyDetectionService _anomalyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AnomalyDetectionBehavior<TRequest, TResponse>> _logger;

    public AnomalyDetectionBehavior(
        AnomalyDetectionService anomalyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AnomalyDetectionBehavior<TRequest, TResponse>> logger)
    {
        _anomalyService = anomalyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Apenas interceptar commands de movimentação de estoque
        var (shouldEvaluate, movementType, locationId) = ExtractMovementInfo(request);

        if (!shouldEvaluate)
            return await next();

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? httpContext?.User.FindFirstValue("sub")
                     ?? "anonymous";
        var userName = httpContext?.User.FindFirstValue(ClaimTypes.Name)
                       ?? httpContext?.User.FindFirstValue("preferred_username");

        // Avaliar anomalia (non-blocking — não impede a execução)
        try
        {
            await _anomalyService.EvaluateMovementAsync(
                userId, userName, movementType!, locationId, null, cancellationToken);
        }
        catch (Exception ex)
        {
            // Anomaly detection failure should never block business operations
            _logger.LogError(ex, "[AnomalyDetection] Falha ao avaliar anomalia — operação prossegue normalmente.");
        }

        return await next();
    }

    private static (bool ShouldEvaluate, string? MovementType, Guid? LocationId) ExtractMovementInfo(TRequest request)
    {
        return request switch
        {
            AdjustStockCommand cmd => (true, cmd.MovementType.ToString(), null),
            CreateStockMovementCommand cmd => (true, cmd.MovementType.ToString(), cmd.LocationId),
            _ => (false, null, null)
        };
    }
}
