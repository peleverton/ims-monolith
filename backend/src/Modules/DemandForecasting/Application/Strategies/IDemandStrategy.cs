using IMS.Modular.Modules.DemandForecasting.Domain.Entities;

namespace IMS.Modular.Modules.DemandForecasting.Application.Strategies;

/// <summary>
/// Strategy Pattern: interface para diferentes algoritmos de cálculo de demanda.
/// </summary>
public interface IDemandStrategy
{
    string Name { get; }

    /// <summary>
    /// Calcula a demanda média diária baseada nas quantidades de saída por dia na janela.
    /// </summary>
    DemandCalculationResult Calculate(IReadOnlyList<DailyOutflow> outflows, int currentStock);
}

/// <summary>Saída diária de um produto (quantidade consumida naquele dia).</summary>
public record DailyOutflow(DateTime Date, int Quantity);

/// <summary>Resultado intermediário do cálculo de uma estratégia.</summary>
public record DemandCalculationResult(
    decimal WeightedAvgDailyDemand,
    DateTime? EstimatedStockoutDate,
    int? DaysUntilStockout,
    StockoutRisk RiskLevel);
