using IMS.Modular.Modules.Analytics.Application.DTOs;
using IMS.Modular.Shared.Abstractions;
using MediatR;

namespace IMS.Modular.Modules.Analytics.Application.Queries;

// ── US-015: Issue Analytics ───────────────────────────────────────────────

public record GetIssueSummaryQuery() : IRequest<IssueSummaryDto>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-summary";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetIssueTrendsQuery(int Days = 30) : IRequest<IReadOnlyList<IssueTrendDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-trends";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetIssueResolutionTimeQuery() : IRequest<IReadOnlyList<IssueResolutionTimeDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-resolution-time";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetIssueStatsByStatusQuery() : IRequest<IReadOnlyList<IssueStatsByStatusDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-stats-status";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetIssueStatsByPriorityQuery() : IRequest<IReadOnlyList<IssueStatsByPriorityDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-stats-priority";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetIssueStatsByAssigneeQuery() : IRequest<IReadOnlyList<IssueStatsByAssigneeDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-stats-assignee";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetAssignSuggestionQuery() : IRequest<IReadOnlyList<AssignSuggestionDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-issue-assign-suggestion";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

// ── US-016: User Workload ─────────────────────────────────────────────────

public record GetAllUsersWorkloadQuery() : IRequest<IReadOnlyList<UserWorkloadSummaryDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-users-workload";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetUserWorkloadQuery(Guid UserId) : IRequest<UserWorkloadDetailDto?>, ICacheable
{
    public string CacheKeyPrefix => "analytics-user-workload";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetUserStatisticsQuery(Guid UserId) : IRequest<UserStatisticsDto?>, ICacheable
{
    public string CacheKeyPrefix => "analytics-user-statistics";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

// ── US-017: Dashboard & Export ────────────────────────────────────────────

public record GetDashboardQuery() : IRequest<DashboardDto>, ICacheable
{
    public string CacheKeyPrefix => "analytics-dashboard";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record ExportDataQuery(
    string Format = "json",
    string? Module = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<ExportResultDto>;

public record ExportResultDto(string FileName, string ContentType, byte[] Data);

// ── US-018: Inventory Analytics ───────────────────────────────────────────

public record GetInventoryValueQuery() : IRequest<InventoryValueDto>, ICacheable
{
    public string CacheKeyPrefix => "analytics-inventory-value";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetInventorySummaryQuery() : IRequest<InventorySummaryDto>, ICacheable
{
    public string CacheKeyPrefix => "analytics-inventory-summary";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetStockStatusDistributionQuery() : IRequest<IReadOnlyList<StockStatusDistributionDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-stock-status";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetStockTrendsQuery(int Days = 30) : IRequest<IReadOnlyList<StockTrendDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-stock-trends";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetCategoryDistributionQuery() : IRequest<IReadOnlyList<CategoryDistributionDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-category-distribution";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetTurnoverRateQuery(int TopN = 20) : IRequest<IReadOnlyList<TurnoverRateDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-turnover-rate";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetExpiringProductsQuery(int DaysAhead = 30, int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<ExpiringProductDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-expiring-products";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetLocationCapacityQuery() : IRequest<IReadOnlyList<LocationCapacityDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-location-capacity";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetSupplierPerformanceQuery() : IRequest<IReadOnlyList<SupplierPerformanceDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-supplier-performance";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record GetTopProductsQuery(int TopN = 10, string OrderBy = "value")
    : IRequest<IReadOnlyList<TopProductDto>>, ICacheable
{
    public string CacheKeyPrefix => "analytics-top-products";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}
