namespace IMS.Modular.Modules.Analytics.Application.DTOs;

// ── US-015: Issue Analytics ───────────────────────────────────────────────

public record IssueSummaryDto(
    int Total,
    int Open,
    int InProgress,
    int Testing,
    int Resolved,
    int Closed,
    int Overdue,
    int DueToday);

// Dapper-friendly: class with settable properties (handles SQLite long→int mapping)
public class IssueTrendDto
{
    public string Date { get; set; } = "";
    public int Created { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
}

public class IssueResolutionTimeDto
{
    public string Priority { get; set; } = "";
    public double AvgResolutionHours { get; set; }
    public double MinResolutionHours { get; set; }
    public double MaxResolutionHours { get; set; }
    public int SampleSize { get; set; }
}

public class IssueStatsByStatusDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class IssueStatsByPriorityDto
{
    public string Priority { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class IssueStatsByAssigneeDto
{
    public Guid? AssigneeId { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Total { get; set; }
}

public class AssignSuggestionDto
{
    public Guid UserId { get; set; }
    public int CurrentLoad { get; set; }
    public int Resolved { get; set; }
    public double AvgResolutionHours { get; set; }
}

// ── US-016: User Workload Analytics ──────────────────────────────────────

public class UserWorkloadSummaryDto
{
    public Guid UserId { get; set; }
    public int TotalAssigned { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Overdue { get; set; }
}

public class UserWorkloadDetailDto
{
    public Guid UserId { get; set; }
    public int TotalAssigned { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Overdue { get; set; }
    public double AvgResolutionHours { get; set; }
    public double CompletionRate { get; set; }
}

public class UserStatisticsDto
{
    public Guid UserId { get; set; }
    public int TotalResolved { get; set; }
    public double AvgResolutionHours { get; set; }
    public double CompletionRate { get; set; }
    public int CurrentLoad { get; set; }
}

// ── US-017: Dashboard & Export ────────────────────────────────────────────

public record DashboardDto(
    IssueSummaryDto IssueSummary,
    InventorySummaryDto InventorySummary,
    IReadOnlyList<IssueTrendDto> RecentTrends,
    IReadOnlyList<UserWorkloadSummaryDto> TopAssignees,
    DateTime GeneratedAt);

public record ExportParametersRequest(
    string Format = "json",
    string? Module = null,
    DateTime? From = null,
    DateTime? To = null);

// ── US-018: Inventory Analytics ───────────────────────────────────────────

public record InventoryValueDto(
    decimal TotalValue,
    decimal TotalCostValue,
    IReadOnlyList<CategoryValueDto> ByCategory,
    IReadOnlyList<LocationValueDto> ByLocation);

public class CategoryValueDto
{
    public string Category { get; set; } = "";
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalCostValue { get; set; }
}

public class LocationValueDto
{
    public Guid? LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
}

public record InventorySummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int DiscontinuedProducts,
    int LowStockProducts,
    int OutOfStockProducts,
    int OverstockProducts,
    decimal TotalInventoryValue);

public class StockStatusDistributionDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class StockTrendDto
{
    public string Date { get; set; } = "";
    public int TotalIn { get; set; }
    public int TotalOut { get; set; }
    public int NetChange { get; set; }
}

public class CategoryDistributionDto
{
    public string Category { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TurnoverRateDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string SKU { get; set; } = "";
    public int TotalMovements { get; set; }
    public int TotalOut { get; set; }
    public double TurnoverRate { get; set; }
}

public class ExpiringProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string SKU { get; set; } = "";
    public int CurrentStock { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class LocationCapacityDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public int Capacity { get; set; }
    public int CurrentStock { get; set; }
    public double UtilizationPercent { get; set; }
}

public class SupplierPerformanceDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string SupplierCode { get; set; } = "";
    public int TotalProducts { get; set; }
    public int TotalPurchases { get; set; }
    public decimal TotalPurchaseValue { get; set; }
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string SKU { get; set; } = "";
    public string Category { get; set; } = "";
    public int CurrentStock { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public int TotalMovements { get; set; }
}
