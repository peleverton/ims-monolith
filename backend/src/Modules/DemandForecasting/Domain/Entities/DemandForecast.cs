using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.DemandForecasting.Domain.Entities;

/// <summary>
/// Previsão de demanda calculada para um produto.
/// Armazena a velocidade de saída e a "data zero" estimada (stockout date).
/// </summary>
public class DemandForecast : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string SKU { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;

    // ── Métricas de velocidade ──────────────────────────────────────
    /// <summary>Média de unidades saindo por dia nos últimos 7 dias.</summary>
    public decimal AvgDailyDemand7d { get; private set; }

    /// <summary>Média de unidades saindo por dia nos últimos 14 dias.</summary>
    public decimal AvgDailyDemand14d { get; private set; }

    /// <summary>Média de unidades saindo por dia nos últimos 30 dias.</summary>
    public decimal AvgDailyDemand30d { get; private set; }

    /// <summary>Média ponderada (janelas mais recentes pesam mais).</summary>
    public decimal WeightedAvgDailyDemand { get; private set; }

    // ── Previsão ────────────────────────────────────────────────────
    /// <summary>Estoque atual no momento do cálculo.</summary>
    public int CurrentStock { get; private set; }

    /// <summary>Data estimada de ruptura (stockout). Null = sem consumo recente.</summary>
    public DateTime? EstimatedStockoutDate { get; private set; }

    /// <summary>Dias estimados até ruptura. Null = sem consumo recente.</summary>
    public int? DaysUntilStockout { get; private set; }

    // ── Classificação ───────────────────────────────────────────────
    /// <summary>Nível de risco: Low, Medium, High, Critical.</summary>
    public StockoutRisk RiskLevel { get; private set; }

    /// <summary>Estratégia usada no cálculo.</summary>
    public string Strategy { get; private set; } = null!;

    /// <summary>Timestamp do cálculo.</summary>
    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

    private DemandForecast() { }

    public DemandForecast(
        Guid productId,
        string sku,
        string productName,
        decimal avgDailyDemand7d,
        decimal avgDailyDemand14d,
        decimal avgDailyDemand30d,
        decimal weightedAvgDailyDemand,
        int currentStock,
        DateTime? estimatedStockoutDate,
        int? daysUntilStockout,
        StockoutRisk riskLevel,
        string strategy)
    {
        ProductId = productId;
        SKU = sku;
        ProductName = productName;
        AvgDailyDemand7d = avgDailyDemand7d;
        AvgDailyDemand14d = avgDailyDemand14d;
        AvgDailyDemand30d = avgDailyDemand30d;
        WeightedAvgDailyDemand = weightedAvgDailyDemand;
        CurrentStock = currentStock;
        EstimatedStockoutDate = estimatedStockoutDate;
        DaysUntilStockout = daysUntilStockout;
        RiskLevel = riskLevel;
        Strategy = strategy;
    }
}

public enum StockoutRisk
{
    /// <summary>Mais de 30 dias de estoque.</summary>
    Low,
    /// <summary>Entre 14 e 30 dias de estoque.</summary>
    Medium,
    /// <summary>Entre 7 e 14 dias de estoque.</summary>
    High,
    /// <summary>Menos de 7 dias de estoque ou já em ruptura.</summary>
    Critical
}
