using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.InventoryIssues.Application.Queries;

public record GetInventoryIssueByIdQuery(Guid Id) : IRequest<InventoryIssueReadDto?>;

public record GetInventoryIssuesQuery(
    int Page = 1,
    int PageSize = 20,
    InventoryIssueStatus? Status = null,
    InventoryIssuePriority? Priority = null,
    InventoryIssueType? Type = null,
    Guid? ProductId = null,
    Guid? LocationId = null,
    Guid? ReporterId = null,
    Guid? AssigneeId = null,
    string? Search = null) : IRequest<PagedResult<InventoryIssueSummaryDto>>, ICacheable
{
    public string CacheKeyPrefix => "inventory-issues-list";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record GetOverdueInventoryIssuesQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<InventoryIssueSummaryDto>>, ICacheable
{
    public string CacheKeyPrefix => "inventory-issues-overdue";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record GetHighPriorityInventoryIssuesQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<InventoryIssueSummaryDto>>, ICacheable
{
    public string CacheKeyPrefix => "inventory-issues-high-priority";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record GetInventoryIssueStatisticsQuery() : IRequest<InventoryIssueStatsDto>, ICacheable
{
    public string CacheKeyPrefix => "inventory-issues-stats";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
