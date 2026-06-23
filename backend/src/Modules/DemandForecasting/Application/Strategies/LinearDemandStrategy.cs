using IMS.Modular.Modules.DemandForecasting.Domain.Entities;

namespace IMS.Modular.Modules.DemandForecasting.Application.Strategies;

/// <summary>
/// Estratégia linear: média aritmética simples da janela.
/// Adequada para produtos com demanda estável e previsível.
/// </summary>
public sealed class LinearDemandStrategy : IDemandStrategy
{
    public string Name => "Linear";

    public DemandCalculationResult Calculate(IReadOnlyList<DailyOutflow> outflows, int currentStock)
    {
        if (outflows.Count == 0 || outflows.All(o => o.Quantity == 0))
        {
            return new DemandCalculationResult(0, null, null, StockoutRisk.Low);
        }

        var totalDays = outflows.Count;
        var totalQuantity = outflows.Sum(o => (decimal)o.Quantity);
        var avgDaily = totalQuantity / totalDays;

        return BuildResult(avgDaily, currentStock);
    }

    private static DemandCalculationResult BuildResult(decimal avgDaily, int currentStock)
    {
        if (avgDaily <= 0)
            return new DemandCalculationResult(0, null, null, StockoutRisk.Low);

        var daysUntilStockout = (int)Math.Floor(currentStock / avgDaily);
        var stockoutDate = DateTime.UtcNow.Date.AddDays(daysUntilStockout);

        var risk = daysUntilStockout switch
        {
            <= 0 => StockoutRisk.Critical,
            <= 7 => StockoutRisk.Critical,
            <= 14 => StockoutRisk.High,
            <= 30 => StockoutRisk.Medium,
            _ => StockoutRisk.Low
        };

        return new DemandCalculationResult(avgDaily, stockoutDate, daysUntilStockout, risk);
    }
}
