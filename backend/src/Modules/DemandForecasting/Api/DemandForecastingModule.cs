using IMS.Modular.Modules.DemandForecasting.Application.DTOs;
using IMS.Modular.Modules.DemandForecasting.Application.Jobs;
using IMS.Modular.Modules.DemandForecasting.Application.Services;
using IMS.Modular.Modules.DemandForecasting.Domain;
using IMS.Modular.Modules.DemandForecasting.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.DemandForecasting.Api;

/// <summary>
/// Epic 1: Demand Forecaster — Motor de previsão e alerta de ruptura de estoque.
/// </summary>
public static class DemandForecastingModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/demand-forecasting")
            .WithTags("Demand Forecasting")
            .RequireAuthorization(Policies.CanViewInventory);

        group.MapGet("/forecasts", async (
            IDemandForecastRepository repo,
            [FromQuery] int top = 50,
            CancellationToken ct = default) =>
        {
            var forecasts = await repo.GetLatestForecastsAsync(top, ct);
            return Results.Ok(forecasts.Select(ToDto).ToList());
        }).WithName("GetDemandForecasts");

        group.MapGet("/forecasts/product/{productId:guid}", async (
            Guid productId,
            IDemandForecastRepository repo,
            CancellationToken ct) =>
        {
            var forecast = await repo.GetByProductAsync(productId, ct);
            return forecast is null ? Results.NotFound() : Results.Ok(ToDto(forecast));
        }).WithName("GetDemandForecastByProduct");

        group.MapGet("/forecasts/risk/{risk}", async (
            StockoutRisk risk,
            IDemandForecastRepository repo,
            CancellationToken ct) =>
        {
            var forecasts = await repo.GetByRiskLevelAsync(risk, ct);
            return Results.Ok(forecasts.Select(ToDto).ToList());
        }).WithName("GetDemandForecastsByRisk");

        group.MapGet("/summary", async (IDemandForecastRepository repo, CancellationToken ct) =>
        {
            var all = await repo.GetLatestForecastsAsync(int.MaxValue, ct);
            var summary = new StockoutRiskSummaryDto(
                Critical: all.Count(f => f.RiskLevel == StockoutRisk.Critical),
                High: all.Count(f => f.RiskLevel == StockoutRisk.High),
                Medium: all.Count(f => f.RiskLevel == StockoutRisk.Medium),
                Low: all.Count(f => f.RiskLevel == StockoutRisk.Low),
                TotalProducts: all.Count);
            return Results.Ok(summary);
        }).WithName("GetStockoutRiskSummary");

        // Trigger manual: recalcular previsões agora (admin only)
        group.MapPost("/recalculate", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<DemandForecastJob>(job => job.ExecuteAsync());
            return Results.Accepted(value: new { message = "Recálculo de previsões enfileirado." });
        }).WithName("TriggerDemandRecalculation")
          .RequireAuthorization(Policies.CanManageInventory);

        return endpoints;
    }

    private static DemandForecastDto ToDto(DemandForecast f) => new(
        f.Id, f.ProductId, f.SKU, f.ProductName,
        f.AvgDailyDemand7d, f.AvgDailyDemand14d, f.AvgDailyDemand30d,
        f.WeightedAvgDailyDemand, f.CurrentStock,
        f.EstimatedStockoutDate, f.DaysUntilStockout,
        f.RiskLevel, f.Strategy, f.CalculatedAt);
}
