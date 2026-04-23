using IMS.Modular.Modules.Analytics.Application.DTOs;
using IMS.Modular.Modules.Analytics.Application.Queries;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.Analytics.Api;

public class AnalyticsModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        MapIssueAnalytics(endpoints);
        MapUserAnalytics(endpoints);
        MapDashboard(endpoints);
        MapInventoryAnalytics(endpoints);
        return endpoints;
    }

    // ── US-015: Issue Analytics (/api/analytics/issues) ───────────────────

    private static void MapIssueAnalytics(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/analytics/issues")
            .WithTags("Analytics - Issues")
            .RequireAuthorization(Policies.CanViewAnalytics);

        group.MapGet("/summary", GetIssueSummary).WithName("GetIssueSummary");
        group.MapGet("/trends", GetIssueTrends).WithName("GetIssueTrends");
        group.MapGet("/resolution-time", GetIssueResolutionTime).WithName("GetIssueResolutionTime");
        group.MapGet("/by-status", GetIssuesByStatus).WithName("GetIssueStatsByStatus");
        group.MapGet("/by-priority", GetIssuesByPriority).WithName("GetIssueStatsByPriority");
        group.MapGet("/by-assignee", GetIssuesByAssignee).WithName("GetIssueStatsByAssignee");
        group.MapGet("/suggest-assignee", GetAssignSuggestion).WithName("GetAssignSuggestion");
        group.MapGet("/statistics", GetIssueStatsByStatus).WithName("GetIssueStatistics");
    }

    private static async Task<IResult> GetIssueSummary(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueSummaryQuery(), ct));

    private static async Task<IResult> GetIssueTrends(
        IMediator mediator, [FromQuery] int days = 30, CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetIssueTrendsQuery(days), ct));

    private static async Task<IResult> GetIssueResolutionTime(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueResolutionTimeQuery(), ct));

    private static async Task<IResult> GetIssuesByStatus(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueStatsByStatusQuery(), ct));

    private static async Task<IResult> GetIssuesByPriority(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueStatsByPriorityQuery(), ct));

    private static async Task<IResult> GetIssuesByAssignee(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueStatsByAssigneeQuery(), ct));

    private static async Task<IResult> GetAssignSuggestion(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetAssignSuggestionQuery(), ct));

    private static async Task<IResult> GetIssueStatsByStatus(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetIssueStatsByStatusQuery(), ct));

    // ── US-016: User Workload (/api/analytics/users) ──────────────────────

    private static void MapUserAnalytics(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/analytics/users")
            .WithTags("Analytics - Users")
            .RequireAuthorization(Policies.CanViewAnalytics);

        group.MapGet("/workload", GetAllUsersWorkload).WithName("GetAllUsersWorkload");
        group.MapGet("/workload/{userId:guid}", GetUserWorkload).WithName("GetUserWorkload");
        group.MapGet("/{userId:guid}/statistics", GetUserStatistics).WithName("GetUserStatistics");
    }

    private static async Task<IResult> GetAllUsersWorkload(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetAllUsersWorkloadQuery(), ct));

    private static async Task<IResult> GetUserWorkload(Guid userId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserWorkloadQuery(userId), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetUserStatistics(Guid userId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserStatisticsQuery(userId), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ── US-017: Dashboard & Export (/api/analytics) ───────────────────────

    private static void MapDashboard(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/analytics")
            .WithTags("Analytics - Dashboard")
            .RequireAuthorization(Policies.CanViewAnalytics);

        group.MapGet("/dashboard", GetDashboard).WithName("GetDashboard");
        group.MapGet("/export", ExportData).WithName("ExportDataGet");
        group.MapPost("/export", ExportDataPost).WithName("ExportDataPost");
    }

    private static async Task<IResult> GetDashboard(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetDashboardQuery(), ct));

    private static async Task<IResult> ExportData(
        IMediator mediator,
        [FromQuery] string format = "json",
        [FromQuery] string? module = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ExportDataQuery(format, module, from, to), ct);
        return Results.File(result.Data, result.ContentType, result.FileName);
    }

    private static async Task<IResult> ExportDataPost(
        ExportParametersRequest req, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ExportDataQuery(req.Format, req.Module, req.From, req.To), ct);
        return Results.File(result.Data, result.ContentType, result.FileName);
    }

    // ── US-018: Inventory Analytics (/api/inventory/analytics) ───────────

    private static void MapInventoryAnalytics(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory/analytics")
            .WithTags("Analytics - Inventory")
            .RequireAuthorization(Policies.CanViewAnalytics);

        group.MapGet("/value", GetInventoryValue).WithName("GetInventoryValue");
        group.MapGet("/summary", GetInventorySummary).WithName("GetInventorySummary");
        group.MapGet("/stock-status", GetStockStatusDistribution).WithName("GetStockStatusDistribution");
        group.MapGet("/stock-trends", GetStockTrends).WithName("GetStockTrends");
        group.MapGet("/categories", GetCategoryDistribution).WithName("GetCategoryDistribution");
        group.MapGet("/turnover", GetTurnoverRate).WithName("GetTurnoverRate");
        group.MapGet("/expiring", GetExpiringProducts).WithName("GetExpiringProducts");
        group.MapGet("/location-capacity", GetLocationCapacity).WithName("GetLocationCapacity");
        group.MapGet("/supplier-performance", GetSupplierPerformance).WithName("GetSupplierPerformance");
        group.MapGet("/top-products", GetTopProducts).WithName("GetTopProducts");
    }

    private static async Task<IResult> GetInventoryValue(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetInventoryValueQuery(), ct));

    private static async Task<IResult> GetInventorySummary(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetInventorySummaryQuery(), ct));

    private static async Task<IResult> GetStockStatusDistribution(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetStockStatusDistributionQuery(), ct));

    private static async Task<IResult> GetStockTrends(
        IMediator mediator, [FromQuery] int days = 30, CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetStockTrendsQuery(days), ct));

    private static async Task<IResult> GetCategoryDistribution(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetCategoryDistributionQuery(), ct));

    private static async Task<IResult> GetTurnoverRate(
        IMediator mediator, [FromQuery] int topN = 20, CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetTurnoverRateQuery(topN), ct));

    private static async Task<IResult> GetExpiringProducts(
        IMediator mediator,
        [FromQuery] int daysAhead = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetExpiringProductsQuery(daysAhead, page, pageSize), ct));

    private static async Task<IResult> GetLocationCapacity(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetLocationCapacityQuery(), ct));

    private static async Task<IResult> GetSupplierPerformance(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetSupplierPerformanceQuery(), ct));

    private static async Task<IResult> GetTopProducts(
        IMediator mediator,
        [FromQuery] int topN = 10,
        [FromQuery] string orderBy = "value",
        CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetTopProductsQuery(topN, orderBy), ct));
}
