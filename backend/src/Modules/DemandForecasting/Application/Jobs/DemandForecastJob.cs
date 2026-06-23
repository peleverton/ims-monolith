using IMS.Modular.Modules.DemandForecasting.Application.Services;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.DemandForecasting.Application.Jobs;

/// <summary>
/// Job Hangfire: recalcula previsões de demanda diariamente.
/// Roda às 06:00 UTC para ter dados frescos no início do expediente.
/// </summary>
public sealed class DemandForecastJob
{
    private readonly DemandForecastingService _service;
    private readonly ILogger<DemandForecastJob> _logger;

    public DemandForecastJob(DemandForecastingService service, ILogger<DemandForecastJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[DemandForecastJob] Iniciando recálculo de previsões...");
        try
        {
            await _service.RecalculateAllAsync();
            _logger.LogInformation("[DemandForecastJob] Recálculo concluído com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DemandForecastJob] Erro durante recálculo de previsões.");
            throw; // Hangfire fará retry automático
        }
    }
}
