using IMS.Modular.Modules.DemandForecasting.Domain.Entities;

namespace IMS.Modular.Modules.DemandForecasting.Domain;

public interface IDemandForecastRepository
{
    Task<List<DemandForecast>> GetLatestForecastsAsync(int top = 50, CancellationToken ct = default);
    Task<List<DemandForecast>> GetByRiskLevelAsync(StockoutRisk risk, CancellationToken ct = default);
    Task<DemandForecast?> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task UpsertAsync(DemandForecast forecast, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
