using IMS.Modular.Modules.AnomalyDetection.Domain;
using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IMS.Modular.Modules.AnomalyDetection.Application.Services;

/// <summary>
/// Serviço de detecção de anomalias usando sliding window counters no cache (Redis).
/// Mantém contadores por {userId}:{movementType} com TTL automático.
/// Quando o counter excede o threshold, gera um AnomalyAlert.
/// </summary>
public sealed class AnomalyDetectionService
{
    private readonly ICacheService _cache;
    private readonly IAnomalyAlertRepository _alertRepo;
    private readonly AnomalyThreshold _threshold;
    private readonly ILogger<AnomalyDetectionService> _logger;

    private const string KeyPrefix = "anomaly-counter";

    public AnomalyDetectionService(
        ICacheService cache,
        IAnomalyAlertRepository alertRepo,
        IOptions<AnomalyThreshold> threshold,
        ILogger<AnomalyDetectionService> logger)
    {
        _cache = cache;
        _alertRepo = alertRepo;
        _threshold = threshold.Value;
        _logger = logger;
    }

    /// <summary>
    /// Avalia uma movimentação de estoque contra os thresholds de anomalia.
    /// Retorna o alerta criado, ou null se dentro dos limites normais.
    /// </summary>
    public async Task<AnomalyAlert?> EvaluateMovementAsync(
        string userId,
        string? userName,
        string movementType,
        Guid? locationId,
        string? locationName,
        CancellationToken ct = default)
    {
        // Apenas monitorar tipos suspeitos
        if (!_threshold.SuspiciousMovementTypes.Contains(movementType, StringComparer.OrdinalIgnoreCase))
            return null;

        // Sliding window counter por usuário + tipo
        var userKey = $"{KeyPrefix}:user:{userId}:{movementType}";
        var currentCount = await IncrementCounterAsync(userKey, ct);

        if (currentCount >= _threshold.MaxAdjustmentsPerWindow)
        {
            var severity = currentCount >= _threshold.MaxAdjustmentsPerWindow * 2
                ? AlertSeverity.Critical
                : AlertSeverity.High;

            var alert = new AnomalyAlert(
                userId, userName, locationId, locationName,
                AnomalyType.ExcessiveAdjustments, severity,
                $"Usuário realizou {currentCount} movimentações do tipo '{movementType}' nos últimos {_threshold.WindowMinutes} minutos.",
                currentCount,
                $"{_threshold.WindowMinutes} min");

            await _alertRepo.AddAsync(alert, ct);
            await _alertRepo.SaveChangesAsync(ct);

            _logger.LogWarning(
                "[AnomalyDetection] ALERTA: Usuário {UserId} com {Count} {Type} em {Window}min",
                userId, currentCount, movementType, _threshold.WindowMinutes);

            return alert;
        }

        // Counter por location + tipo (se location informada)
        if (locationId.HasValue)
        {
            var locationKey = $"{KeyPrefix}:location:{locationId}:{movementType}";
            var locationCount = await IncrementCounterAsync(locationKey, ct);

            if (locationCount >= _threshold.MaxLossesPerLocationPerWindow)
            {
                var alert = new AnomalyAlert(
                    userId, userName, locationId, locationName,
                    AnomalyType.ExcessiveLosses, AlertSeverity.High,
                    $"Location '{locationName ?? locationId.ToString()}' teve {locationCount} movimentações do tipo '{movementType}' nos últimos {_threshold.WindowMinutes} minutos.",
                    locationCount,
                    $"{_threshold.WindowMinutes} min");

                await _alertRepo.AddAsync(alert, ct);
                await _alertRepo.SaveChangesAsync(ct);

                _logger.LogWarning(
                    "[AnomalyDetection] ALERTA: Location {LocationId} com {Count} {Type} em {Window}min",
                    locationId, locationCount, movementType, _threshold.WindowMinutes);

                return alert;
            }
        }

        return null;
    }

    /// <summary>
    /// Incrementa um counter no cache com TTL = WindowMinutes.
    /// Simula sliding window: cada key vive apenas pela duração da janela.
    /// </summary>
    private async Task<int> IncrementCounterAsync(string key, CancellationToken ct)
    {
        var current = await _cache.GetAsync<int?>(key, ct) ?? 0;
        var newValue = current + 1;
        var ttl = TimeSpan.FromMinutes(_threshold.WindowMinutes);
        await _cache.SetAsync(key, newValue, ttl, ct);
        return newValue;
    }
}
