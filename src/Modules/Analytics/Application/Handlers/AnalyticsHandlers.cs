using IMS.Modular.Modules.Analytics.Application.DTOs;
using IMS.Modular.Modules.Analytics.Application.Queries;
using IMS.Modular.Modules.Analytics.Infrastructure;
using MediatR;

namespace IMS.Modular.Modules.Analytics.Application.Handlers;

// ── US-015: Issue Analytics ───────────────────────────────────────────────

public class GetIssueSummaryHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueSummaryQuery, IssueSummaryDto>
{
    public Task<IssueSummaryDto> Handle(GetIssueSummaryQuery q, CancellationToken ct)
        => repo.GetIssueSummaryAsync(ct);
}

public class GetIssueTrendsHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueTrendsQuery, IReadOnlyList<IssueTrendDto>>
{
    public Task<IReadOnlyList<IssueTrendDto>> Handle(GetIssueTrendsQuery q, CancellationToken ct)
        => repo.GetIssueTrendsAsync(q.Days, ct);
}

public class GetIssueResolutionTimeHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueResolutionTimeQuery, IReadOnlyList<IssueResolutionTimeDto>>
{
    public Task<IReadOnlyList<IssueResolutionTimeDto>> Handle(GetIssueResolutionTimeQuery q, CancellationToken ct)
        => repo.GetIssueResolutionTimeAsync(ct);
}

public class GetIssueStatsByStatusHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueStatsByStatusQuery, IReadOnlyList<IssueStatsByStatusDto>>
{
    public Task<IReadOnlyList<IssueStatsByStatusDto>> Handle(GetIssueStatsByStatusQuery q, CancellationToken ct)
        => repo.GetIssueStatsByStatusAsync(ct);
}

public class GetIssueStatsByPriorityHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueStatsByPriorityQuery, IReadOnlyList<IssueStatsByPriorityDto>>
{
    public Task<IReadOnlyList<IssueStatsByPriorityDto>> Handle(GetIssueStatsByPriorityQuery q, CancellationToken ct)
        => repo.GetIssueStatsByPriorityAsync(ct);
}

public class GetIssueStatsByAssigneeHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetIssueStatsByAssigneeQuery, IReadOnlyList<IssueStatsByAssigneeDto>>
{
    public Task<IReadOnlyList<IssueStatsByAssigneeDto>> Handle(GetIssueStatsByAssigneeQuery q, CancellationToken ct)
        => repo.GetIssueStatsByAssigneeAsync(ct);
}

public class GetAssignSuggestionHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetAssignSuggestionQuery, IReadOnlyList<AssignSuggestionDto>>
{
    public Task<IReadOnlyList<AssignSuggestionDto>> Handle(GetAssignSuggestionQuery q, CancellationToken ct)
        => repo.GetAssignSuggestionsAsync(ct);
}

// ── US-016: User Workload ─────────────────────────────────────────────────

public class GetAllUsersWorkloadHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetAllUsersWorkloadQuery, IReadOnlyList<UserWorkloadSummaryDto>>
{
    public Task<IReadOnlyList<UserWorkloadSummaryDto>> Handle(GetAllUsersWorkloadQuery q, CancellationToken ct)
        => repo.GetAllUsersWorkloadAsync(ct);
}

public class GetUserWorkloadHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetUserWorkloadQuery, UserWorkloadDetailDto?>
{
    public Task<UserWorkloadDetailDto?> Handle(GetUserWorkloadQuery q, CancellationToken ct)
        => repo.GetUserWorkloadAsync(q.UserId, ct);
}

public class GetUserStatisticsHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetUserStatisticsQuery, UserStatisticsDto?>
{
    public Task<UserStatisticsDto?> Handle(GetUserStatisticsQuery q, CancellationToken ct)
        => repo.GetUserStatisticsAsync(q.UserId, ct);
}

// ── US-017: Dashboard & Export ────────────────────────────────────────────

public class GetDashboardHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery q, CancellationToken ct)
    {
        var issueSummary = await repo.GetIssueSummaryAsync(ct);
        var inventorySummary = await repo.GetInventorySummaryAsync(ct);
        var trends = await repo.GetIssueTrendsAsync(7, ct);
        var topAssignees = await repo.GetAllUsersWorkloadAsync(ct);

        return new DashboardDto(
            issueSummary,
            inventorySummary,
            trends,
            topAssignees.Take(5).ToList(),
            DateTime.UtcNow);
    }
}

public class ExportDataHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<ExportDataQuery, ExportResultDto>
{
    public async Task<ExportResultDto> Handle(ExportDataQuery q, CancellationToken ct)
    {
        var format = q.Format.ToLower();
        var data = await repo.ExportToJsonAsync(q.Module, q.From, q.To);

        return format switch
        {
            "csv" => new ExportResultDto(
                $"ims-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
                "text/csv",
                data),
            _ => new ExportResultDto(
                $"ims-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json",
                "application/json",
                data)
        };
    }
}

// ── US-018: Inventory Analytics ───────────────────────────────────────────

public class GetInventoryValueHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetInventoryValueQuery, InventoryValueDto>
{
    public Task<InventoryValueDto> Handle(GetInventoryValueQuery q, CancellationToken ct)
        => repo.GetInventoryValueAsync(ct);
}

public class GetInventorySummaryHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetInventorySummaryQuery, InventorySummaryDto>
{
    public Task<InventorySummaryDto> Handle(GetInventorySummaryQuery q, CancellationToken ct)
        => repo.GetInventorySummaryAsync(ct);
}

public class GetStockStatusDistributionHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetStockStatusDistributionQuery, IReadOnlyList<StockStatusDistributionDto>>
{
    public Task<IReadOnlyList<StockStatusDistributionDto>> Handle(GetStockStatusDistributionQuery q, CancellationToken ct)
        => repo.GetStockStatusDistributionAsync(ct);
}

public class GetStockTrendsHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetStockTrendsQuery, IReadOnlyList<StockTrendDto>>
{
    public Task<IReadOnlyList<StockTrendDto>> Handle(GetStockTrendsQuery q, CancellationToken ct)
        => repo.GetStockTrendsAsync(q.Days, ct);
}

public class GetCategoryDistributionHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetCategoryDistributionQuery, IReadOnlyList<CategoryDistributionDto>>
{
    public Task<IReadOnlyList<CategoryDistributionDto>> Handle(GetCategoryDistributionQuery q, CancellationToken ct)
        => repo.GetCategoryDistributionAsync(ct);
}

public class GetTurnoverRateHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetTurnoverRateQuery, IReadOnlyList<TurnoverRateDto>>
{
    public Task<IReadOnlyList<TurnoverRateDto>> Handle(GetTurnoverRateQuery q, CancellationToken ct)
        => repo.GetTurnoverRateAsync(q.TopN, ct);
}

public class GetExpiringProductsHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetExpiringProductsQuery, IReadOnlyList<ExpiringProductDto>>
{
    public Task<IReadOnlyList<ExpiringProductDto>> Handle(GetExpiringProductsQuery q, CancellationToken ct)
        => repo.GetExpiringProductsAsync(q.DaysAhead, q.Page, q.PageSize, ct);
}

public class GetLocationCapacityHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetLocationCapacityQuery, IReadOnlyList<LocationCapacityDto>>
{
    public Task<IReadOnlyList<LocationCapacityDto>> Handle(GetLocationCapacityQuery q, CancellationToken ct)
        => repo.GetLocationCapacityAsync(ct);
}

public class GetSupplierPerformanceHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetSupplierPerformanceQuery, IReadOnlyList<SupplierPerformanceDto>>
{
    public Task<IReadOnlyList<SupplierPerformanceDto>> Handle(GetSupplierPerformanceQuery q, CancellationToken ct)
        => repo.GetSupplierPerformanceAsync(ct);
}

public class GetTopProductsHandler(IAnalyticsReadRepository repo)
    : IRequestHandler<GetTopProductsQuery, IReadOnlyList<TopProductDto>>
{
    public Task<IReadOnlyList<TopProductDto>> Handle(GetTopProductsQuery q, CancellationToken ct)
        => repo.GetTopProductsAsync(q.TopN, q.OrderBy, ct);
}
