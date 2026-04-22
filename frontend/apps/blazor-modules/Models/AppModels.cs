namespace BlazorModules.Models;

// ─── Issues ───────────────────────────────────────────────────────────────────

public enum IssueStatus { Open, InProgress, Resolved, Closed }
public enum IssuePriority { Low, Medium, High, Critical }

public record IssueDto(
    Guid Id,
    string Title,
    string Description,
    IssueStatus Status,
    IssuePriority Priority,
    string? AssigneeName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Tags,
    int CommentsCount
);

// ─── Inventory ────────────────────────────────────────────────────────────────

public enum ProductStatus { Active, Inactive, Discontinued }

public record InventoryItemDto(
    Guid Id,
    string Name,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    string? Location,
    ProductStatus Status,
    int ReorderPoint,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class CreateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Location { get; set; }
    public int ReorderPoint { get; set; } = 10;
}

public record UpdateInventoryItemRequest(
    string? Name,
    int? Quantity,
    decimal? UnitPrice,
    string? Location,
    ProductStatus? Status
);

// ─── Analytics ────────────────────────────────────────────────────────────────

/// <summary>DTO consolidado usado pelo AnalyticsDashboard.razor, mapeado a partir de DashboardDto do backend.</summary>
public record AnalyticsSummaryDto(
    int TotalIssues,
    int OpenIssues,
    int InProgressIssues,
    int ResolvedIssues,
    int ClosedIssues,
    int TotalInventoryItems,
    int LowStockItems,
    Dictionary<string, int> IssuesByStatus,
    Dictionary<string, int> IssuesByPriority,
    List<IssuesByDayDto> IssuesByDay
);

public record IssuesByDayDto(string Date, int Count);

// ─── Backend DTOs (mapeamento de /api/analytics/dashboard) ───────────────────

public record BackendDashboardDto(
    BackendIssueSummaryDto IssueSummary,
    BackendInventorySummaryDto InventorySummary,
    List<BackendIssueTrendDto> RecentTrends,
    List<BackendUserWorkloadDto> TopAssignees,
    DateTime GeneratedAt
);

public record BackendIssueSummaryDto(
    int Total,
    int Open,
    int InProgress,
    int Testing,
    int Resolved,
    int Closed,
    int Overdue,
    int DueToday
);

public record BackendInventorySummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int DiscontinuedProducts,
    int LowStockProducts,
    int OutOfStockProducts,
    int OverstockProducts,
    decimal TotalInventoryValue
);

public record BackendIssueTrendDto(string Date, int Created, int Resolved, int Closed);

public record BackendUserWorkloadDto(
    Guid UserId,
    int TotalAssigned,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int Overdue
);

public record BackendIssueStatsByStatusDto(string Status, int Count, double Percentage);
public record BackendIssueStatsByPriorityDto(string Priority, int Count, double Percentage);

// ─── Pagination ───────────────────────────────────────────────────────────────

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
