using IMS.Modular.Modules.DemandForecasting.Domain.Entities;

namespace IMS.Modular.Modules.DemandForecasting.Application.Strategies;

/// <summary>
/// Estratégia de Média Móvel Ponderada (Weighted Moving Average).
/// Dias mais recentes têm peso maior, capturando tendências de aceleração/desaceleração.
/// Peso exponencial: dia mais recente = peso máximo, decai linearmente.
/// </summary>
public sealed class WeightedMovingAvgStrategy : IDemandStrategy
{
    public string Name => "WeightedMovingAvg";

    public DemandCalculationResult Calculate(IReadOnlyList<DailyOutflow> outflows, int currentStock)
    {
        if (outflows.Count == 0 || outflows.All(o => o.Quantity == 0))
        {
            return new DemandCalculationResult(0, null, null, StockoutRisk.Low);
        }

        // Ordenar por data crescente (mais antigo primeiro)
        var sorted = outflows.OrderBy(o => o.Date).ToList();
        var n = sorted.Count;

        // Pesos lineares: dia 1 (mais antigo) = peso 1, dia N (mais recente) = peso N
        decimal totalWeight = 0;
        decimal weightedSum = 0;

        for (int i = 0; i < n; i++)
        {
            var weight = i + 1; // 1, 2, 3, ..., N
            weightedSum += sorted[i].Quantity * weight;
            totalWeight += weight;
        }

        var avgDaily = weightedSum / totalWeight;

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
