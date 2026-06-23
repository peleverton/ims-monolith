using IMS.Modular.Modules.DemandForecasting.Application.Strategies;
using IMS.Modular.Modules.DemandForecasting.Domain;
using IMS.Modular.Modules.DemandForecasting.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.DemandForecasting.Application.Services;

/// <summary>
/// Serviço principal que calcula as previsões de demanda para todos os produtos ativos.
/// Usa Sliding Window sobre StockMovements para determinar velocidade de saída.
/// </summary>
public sealed class DemandForecastingService
{
    private readonly InventoryDbContext _inventory;
    private readonly IDemandForecastRepository _forecastRepo;
    private readonly IDemandStrategy _strategy;
    private readonly ILogger<DemandForecastingService> _logger;

    private static readonly StockMovementType[] OutgoingTypes =
    [
        StockMovementType.StockOut,
        StockMovementType.Sale,
        StockMovementType.Damage,
        StockMovementType.Loss,
        StockMovementType.Expired
    ];

    public DemandForecastingService(
        InventoryDbContext inventory,
        IDemandForecastRepository forecastRepo,
        IEnumerable<IDemandStrategy> strategies,
        ILogger<DemandForecastingService> logger)
    {
        _inventory = inventory;
        _forecastRepo = forecastRepo;
        // Default: WeightedMovingAvg (mais precisa para uso geral)
        _strategy = strategies.FirstOrDefault(s => s.Name == "WeightedMovingAvg")
                    ?? strategies.First();
        _logger = logger;
    }

    /// <summary>
    /// Recalcula as previsões para todos os produtos ativos com estoque > 0.
    /// </summary>
    public async Task RecalculateAllAsync(CancellationToken ct = default)
    {
        var activeProducts = await _inventory.Products
            .Where(p => p.IsActive && p.CurrentStock > 0)
            .Select(p => new { p.Id, p.SKU, p.Name, p.CurrentStock })
            .ToListAsync(ct);

        _logger.LogInformation("[DemandForecasting] Recalculando previsões para {Count} produtos", activeProducts.Count);

        var now = DateTime.UtcNow.Date;
        var window30d = now.AddDays(-30);

        // Buscar todos os movimentos de saída dos últimos 30 dias de uma vez (batch)
        var movements = await _inventory.StockMovements
            .Where(m => m.MovementDate >= window30d && OutgoingTypes.Contains(m.MovementType))
            .Select(m => new MovementRecord(m.ProductId, Math.Abs(m.Quantity), m.MovementDate))
            .ToListAsync(ct);

        // Agrupar por produto
        var movementsByProduct = movements
            .GroupBy(m => m.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var forecastCount = 0;

        foreach (var product in activeProducts)
        {
            var productMovements = movementsByProduct.GetValueOrDefault(product.Id);

            // Construir série diária (sliding window de 30 dias)
            var dailyOutflows = BuildDailyOutflows(productMovements, window30d, now);

            // Calcular métricas por janela
            var avg7d = CalculateWindowAvg(dailyOutflows, 7);
            var avg14d = CalculateWindowAvg(dailyOutflows, 14);
            var avg30d = CalculateWindowAvg(dailyOutflows, 30);

            // Calcular previsão usando estratégia (sobre janela de 30 dias)
            var result = _strategy.Calculate(dailyOutflows, product.CurrentStock);

            var forecast = new DemandForecast(
                product.Id,
                product.SKU,
                product.Name,
                avg7d,
                avg14d,
                avg30d,
                result.WeightedAvgDailyDemand,
                product.CurrentStock,
                result.EstimatedStockoutDate,
                result.DaysUntilStockout,
                result.RiskLevel,
                _strategy.Name);

            await _forecastRepo.UpsertAsync(forecast, ct);
            forecastCount++;
        }

        await _forecastRepo.SaveChangesAsync(ct);
        _logger.LogInformation("[DemandForecasting] {Count} previsões calculadas com estratégia '{Strategy}'",
            forecastCount, _strategy.Name);
    }

    private static List<DailyOutflow> BuildDailyOutflows(
        List<MovementRecord>? movements,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var outflows = new List<DailyOutflow>();

        for (var date = windowStart; date <= windowEnd; date = date.AddDays(1))
        {
            var qty = 0;
            if (movements is not null)
            {
                qty = movements
                    .Where(m => m.MovementDate.Date == date)
                    .Sum(m => m.Quantity);
            }
            outflows.Add(new DailyOutflow(date, qty));
        }

        return outflows;
    }

    private static decimal CalculateWindowAvg(List<DailyOutflow> allOutflows, int windowDays)
    {
        var windowData = allOutflows.TakeLast(windowDays).ToList();
        if (windowData.Count == 0) return 0;
        return windowData.Sum(o => (decimal)o.Quantity) / windowData.Count;
    }

    private sealed record MovementRecord(Guid ProductId, int Quantity, DateTime MovementDate);
}
