using IMS.Modular.Modules.DemandForecasting.Domain.Entities;

namespace IMS.Modular.Modules.DemandForecasting.Application.DTOs;

public record DemandForecastDto(
    Guid Id,
    Guid ProductId,
    string SKU,
    string ProductName,
    decimal AvgDailyDemand7d,
    decimal AvgDailyDemand14d,
    decimal AvgDailyDemand30d,
    decimal WeightedAvgDailyDemand,
    int CurrentStock,
    DateTime? EstimatedStockoutDate,
    int? DaysUntilStockout,
    StockoutRisk RiskLevel,
    string Strategy,
    DateTime CalculatedAt);

public record StockoutRiskSummaryDto(
    int Critical,
    int High,
    int Medium,
    int Low,
    int TotalProducts);
