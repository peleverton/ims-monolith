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

public record IssueTrendDto(
    string Date,
    int Created,
    int Resolved,
    int Closed);

public record IssueResolutionTimeDto(
    string Priority,
    double AvgResolutionHours,
    double MinResolutionHours,
    double MaxResolutionHours,
    int SampleSize);

public record IssueStatsByStatusDto(string Status, int Count, double Percentage);
public record IssueStatsByPriorityDto(string Priority, int Count, double Percentage);
public record IssueStatsByAssigneeDto(Guid? AssigneeId, int Open, int InProgress, int Resolved, int Closed, int Total);
public record AssignSuggestionDto(Guid UserId, int CurrentLoad, int Resolved, double AvgResolutionHours);

// ── US-016: User Workload Analytics ──────────────────────────────────────

public record UserWorkloadSummaryDto(
    Guid UserId,
    int TotalAssigned,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int Overdue);

public record UserWorkloadDetailDto(
    Guid UserId,
    int TotalAssigned,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int Overdue,
    double AvgResolutionHours,
    double CompletionRate);

public record UserStatisticsDto(
    Guid UserId,
    int TotalResolved,
    double AvgResolutionHours,
    double CompletionRate,
    int CurrentLoad);

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

public record CategoryValueDto(string Category, int ProductCount, decimal TotalValue, decimal TotalCostValue);
public record LocationValueDto(Guid? LocationId, string LocationName, int ProductCount, decimal TotalValue);

public record InventorySummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int DiscontinuedProducts,
    int LowStockProducts,
    int OutOfStockProducts,
    int OverstockProducts,
    decimal TotalInventoryValue);

public record StockStatusDistributionDto(string Status, int Count, double Percentage);

public record StockTrendDto(string Date, int TotalIn, int TotalOut, int NetChange);

public record CategoryDistributionDto(string Category, int Count, double Percentage);

public record TurnoverRateDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    int TotalMovements,
    int TotalOut,
    double TurnoverRate);

public record ExpiringProductDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    int CurrentStock,
    DateTime ExpiryDate,
    int DaysUntilExpiry);

public record LocationCapacityDto(
    Guid LocationId,
    string LocationName,
    string LocationCode,
    int Capacity,
    int CurrentStock,
    double UtilizationPercent);

public record SupplierPerformanceDto(
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    int TotalProducts,
    int TotalPurchases,
    decimal TotalPurchaseValue);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    string Category,
    int CurrentStock,
    decimal UnitPrice,
    decimal TotalValue,
    int TotalMovements);
