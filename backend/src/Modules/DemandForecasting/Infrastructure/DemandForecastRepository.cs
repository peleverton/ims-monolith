using IMS.Modular.Modules.DemandForecasting.Domain;
using IMS.Modular.Modules.DemandForecasting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.DemandForecasting.Infrastructure;

public sealed class DemandForecastRepository(DemandForecastingDbContext db) : IDemandForecastRepository
{
    public async Task<List<DemandForecast>> GetLatestForecastsAsync(int top = 50, CancellationToken ct = default)
        => await db.DemandForecasts
            .OrderBy(f => f.DaysUntilStockout ?? int.MaxValue)
            .ThenByDescending(f => (double)f.WeightedAvgDailyDemand)
            .Take(top)
            .ToListAsync(ct);

    public async Task<List<DemandForecast>> GetByRiskLevelAsync(StockoutRisk risk, CancellationToken ct = default)
        => await db.DemandForecasts
            .Where(f => f.RiskLevel == risk)
            .OrderBy(f => f.DaysUntilStockout ?? int.MaxValue)
            .ToListAsync(ct);

    public async Task<DemandForecast?> GetByProductAsync(Guid productId, CancellationToken ct = default)
        => await db.DemandForecasts
            .FirstOrDefaultAsync(f => f.ProductId == productId, ct);

    public async Task UpsertAsync(DemandForecast forecast, CancellationToken ct = default)
    {
        var existing = await db.DemandForecasts
            .FirstOrDefaultAsync(f => f.ProductId == forecast.ProductId, ct);

        if (existing is not null)
        {
            db.DemandForecasts.Remove(existing);
        }

        await db.DemandForecasts.AddAsync(forecast, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
