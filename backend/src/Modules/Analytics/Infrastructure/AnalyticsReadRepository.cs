using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using IMS.Modular.Modules.Analytics.Application.DTOs;

namespace IMS.Modular.Modules.Analytics.Infrastructure;

file static class GH
{
    internal static string Up(Guid id) => id.ToString().ToUpperInvariant();
}

public interface IAnalyticsReadRepository
{
    // Issue Analytics (US-015)
    Task<IssueSummaryDto> GetIssueSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueTrendDto>> GetIssueTrendsAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<IssueResolutionTimeDto>> GetIssueResolutionTimeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueStatsByStatusDto>> GetIssueStatsByStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueStatsByPriorityDto>> GetIssueStatsByPriorityAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueStatsByAssigneeDto>> GetIssueStatsByAssigneeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AssignSuggestionDto>> GetAssignSuggestionsAsync(CancellationToken ct = default);

    // User Workload (US-016)
    Task<IReadOnlyList<UserWorkloadSummaryDto>> GetAllUsersWorkloadAsync(CancellationToken ct = default);
    Task<UserWorkloadDetailDto?> GetUserWorkloadAsync(Guid userId, CancellationToken ct = default);
    Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken ct = default);

    // Inventory Analytics (US-018)
    Task<InventoryValueDto> GetInventoryValueAsync(CancellationToken ct = default);
    Task<InventorySummaryDto> GetInventorySummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockStatusDistributionDto>> GetStockStatusDistributionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockTrendDto>> GetStockTrendsAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDistributionDto>> GetCategoryDistributionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TurnoverRateDto>> GetTurnoverRateAsync(int topN, CancellationToken ct = default);
    Task<IReadOnlyList<ExpiringProductDto>> GetExpiringProductsAsync(int daysAhead, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<LocationCapacityDto>> GetLocationCapacityAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SupplierPerformanceDto>> GetSupplierPerformanceAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int topN, string orderBy, CancellationToken ct = default);

    // Export (US-017)
    Task<byte[]> ExportToJsonAsync(string? module, DateTime? from, DateTime? to);
}

public class AnalyticsReadRepository(IDbConnection connection) : IAnalyticsReadRepository
{
    // ── Issue Analytics ───────────────────────────────────────────────────

    public async Task<IssueSummaryDto> GetIssueSummaryAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*)                                                                                          AS Total,
                SUM(CASE WHEN "Status" = 'Open'        THEN 1 ELSE 0 END)                                        AS Open,
                SUM(CASE WHEN "Status" = 'InProgress'  THEN 1 ELSE 0 END)                                        AS InProgress,
                SUM(CASE WHEN "Status" = 'Testing'     THEN 1 ELSE 0 END)                                        AS Testing,
                SUM(CASE WHEN "Status" = 'Resolved'    THEN 1 ELSE 0 END)                                        AS Resolved,
                SUM(CASE WHEN "Status" = 'Closed'      THEN 1 ELSE 0 END)                                        AS Closed,
                SUM(CASE WHEN "DueDate" < datetime('now') AND "Status" NOT IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS Overdue,
                SUM(CASE WHEN date("DueDate") = date('now') AND "Status" NOT IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS DueToday
            FROM "Issues"
            """;
        var row = await connection.QuerySingleAsync<dynamic>(sql);
        return new IssueSummaryDto(
            (int)(row.total ?? 0), (int)(row.open ?? 0), (int)(row.inprogress ?? 0),
            (int)(row.testing ?? 0), (int)(row.resolved ?? 0), (int)(row.closed ?? 0),
            (int)(row.overdue ?? 0), (int)(row.duetoday ?? 0));
    }

    public async Task<IReadOnlyList<IssueTrendDto>> GetIssueTrendsAsync(int days, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                strftime('%Y-%m-%d', "CreatedAt")                        AS "Date",
                COUNT(*)                                                  AS "Created",
                SUM(CASE WHEN "Status" = 'Resolved' THEN 1 ELSE 0 END)  AS "Resolved",
                SUM(CASE WHEN "Status" = 'Closed'   THEN 1 ELSE 0 END)  AS "Closed"
            FROM "Issues"
            WHERE "CreatedAt" >= datetime('now', '-{days} days')
            GROUP BY strftime('%Y-%m-%d', "CreatedAt")
            ORDER BY "Date" ASC
            """;
        var rows = await connection.QueryAsync<IssueTrendDto>(sql);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<IssueResolutionTimeDto>> GetIssueResolutionTimeAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "Priority",
                AVG((julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0)  AS "AvgResolutionHours",
                MIN((julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0)  AS "MinResolutionHours",
                MAX((julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0)  AS "MaxResolutionHours",
                COUNT(*)                                                        AS "SampleSize"
            FROM "Issues"
            WHERE "Status" IN ('Resolved', 'Closed') AND "UpdatedAt" IS NOT NULL
            GROUP BY "Priority"
            """;
        var rows = await connection.QueryAsync<IssueResolutionTimeDto>(sql);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<IssueStatsByStatusDto>> GetIssueStatsByStatusAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "Status",
                COUNT(*) AS "Count",
                ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM "Issues"), 2) AS "Percentage"
            FROM "Issues"
            GROUP BY "Status"
            ORDER BY "Count" DESC
            """;
        return (await connection.QueryAsync<IssueStatsByStatusDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<IssueStatsByPriorityDto>> GetIssueStatsByPriorityAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "Priority",
                COUNT(*) AS "Count",
                ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM "Issues"), 2) AS "Percentage"
            FROM "Issues"
            GROUP BY "Priority"
            ORDER BY "Count" DESC
            """;
        return (await connection.QueryAsync<IssueStatsByPriorityDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<IssueStatsByAssigneeDto>> GetIssueStatsByAssigneeAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "AssigneeId",
                SUM(CASE WHEN "Status" = 'Open'       THEN 1 ELSE 0 END) AS "Open",
                SUM(CASE WHEN "Status" = 'InProgress' THEN 1 ELSE 0 END) AS "InProgress",
                SUM(CASE WHEN "Status" = 'Resolved'   THEN 1 ELSE 0 END) AS "Resolved",
                SUM(CASE WHEN "Status" = 'Closed'     THEN 1 ELSE 0 END) AS "Closed",
                COUNT(*) AS "Total"
            FROM "Issues"
            WHERE "AssigneeId" IS NOT NULL
            GROUP BY "AssigneeId"
            ORDER BY "Total" DESC
            """;
        return (await connection.QueryAsync<IssueStatsByAssigneeDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<AssignSuggestionDto>> GetAssignSuggestionsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "AssigneeId" AS "UserId",
                SUM(CASE WHEN "Status" IN ('Open','InProgress') THEN 1 ELSE 0 END)     AS "CurrentLoad",
                SUM(CASE WHEN "Status" IN ('Resolved','Closed') THEN 1 ELSE 0 END)     AS "Resolved",
                AVG(CASE WHEN "Status" IN ('Resolved','Closed') AND "UpdatedAt" IS NOT NULL
                    THEN (julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0 END) AS "AvgResolutionHours"
            FROM "Issues"
            WHERE "AssigneeId" IS NOT NULL
            GROUP BY "AssigneeId"
            ORDER BY "CurrentLoad" ASC, "AvgResolutionHours" ASC
            LIMIT 5
            """;
        return (await connection.QueryAsync<AssignSuggestionDto>(sql)).AsList();
    }

    // ── User Workload ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<UserWorkloadSummaryDto>> GetAllUsersWorkloadAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "AssigneeId" AS "UserId",
                COUNT(*) AS "TotalAssigned",
                SUM(CASE WHEN "Status" = 'Open'       THEN 1 ELSE 0 END) AS "Open",
                SUM(CASE WHEN "Status" = 'InProgress' THEN 1 ELSE 0 END) AS "InProgress",
                SUM(CASE WHEN "Status" = 'Resolved'   THEN 1 ELSE 0 END) AS "Resolved",
                SUM(CASE WHEN "Status" = 'Closed'     THEN 1 ELSE 0 END) AS "Closed",
                SUM(CASE WHEN "DueDate" < datetime('now') AND "Status" NOT IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS "Overdue"
            FROM "Issues"
            WHERE "AssigneeId" IS NOT NULL
            GROUP BY "AssigneeId"
            ORDER BY "TotalAssigned" DESC
            """;
        return (await connection.QueryAsync<UserWorkloadSummaryDto>(sql)).AsList();
    }

    public async Task<UserWorkloadDetailDto?> GetUserWorkloadAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "AssigneeId" AS "UserId",
                COUNT(*) AS "TotalAssigned",
                SUM(CASE WHEN "Status" = 'Open'       THEN 1 ELSE 0 END) AS "Open",
                SUM(CASE WHEN "Status" = 'InProgress' THEN 1 ELSE 0 END) AS "InProgress",
                SUM(CASE WHEN "Status" = 'Resolved'   THEN 1 ELSE 0 END) AS "Resolved",
                SUM(CASE WHEN "Status" = 'Closed'     THEN 1 ELSE 0 END) AS "Closed",
                SUM(CASE WHEN "DueDate" < datetime('now') AND "Status" NOT IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS "Overdue",
                AVG(CASE WHEN "Status" IN ('Resolved','Closed') AND "UpdatedAt" IS NOT NULL
                    THEN (julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0 END) AS "AvgResolutionHours",
                ROUND(SUM(CASE WHEN "Status" IN ('Resolved','Closed') THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS "CompletionRate"
            FROM "Issues"
            WHERE UPPER(CAST("AssigneeId" AS TEXT)) = @UserId
            GROUP BY "AssigneeId"
            """;
        return await connection.QuerySingleOrDefaultAsync<UserWorkloadDetailDto>(sql, new { UserId = GH.Up(userId) });
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "AssigneeId" AS "UserId",
                SUM(CASE WHEN "Status" IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS "TotalResolved",
                AVG(CASE WHEN "Status" IN ('Resolved','Closed') AND "UpdatedAt" IS NOT NULL
                    THEN (julianday("UpdatedAt") - julianday("CreatedAt")) * 24.0 END) AS "AvgResolutionHours",
                ROUND(SUM(CASE WHEN "Status" IN ('Resolved','Closed') THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS "CompletionRate",
                SUM(CASE WHEN "Status" IN ('Open','InProgress') THEN 1 ELSE 0 END) AS "CurrentLoad"
            FROM "Issues"
            WHERE UPPER(CAST("AssigneeId" AS TEXT)) = @UserId
            GROUP BY "AssigneeId"
            """;
        return await connection.QuerySingleOrDefaultAsync<UserStatisticsDto>(sql, new { UserId = GH.Up(userId) });
    }

    // ── Inventory Analytics ───────────────────────────────────────────────

    public async Task<InventoryValueDto> GetInventoryValueAsync(CancellationToken ct = default)
    {
        const string totalSql = """
            SELECT
                SUM("CurrentStock" * "UnitPrice") AS TotalValue,
                SUM("CurrentStock" * "CostPrice") AS TotalCostValue
            FROM "Products" WHERE "IsActive" = 1
            """;
        var total = await connection.QuerySingleAsync<dynamic>(totalSql);

        const string byCatSql = """
            SELECT
                "Category",
                COUNT(*) AS ProductCount,
                SUM("CurrentStock" * "UnitPrice") AS TotalValue,
                SUM("CurrentStock" * "CostPrice") AS TotalCostValue
            FROM "Products" WHERE "IsActive" = 1
            GROUP BY "Category" ORDER BY TotalValue DESC
            """;
        var byCategory = (await connection.QueryAsync<CategoryValueDto>(byCatSql)).AsList();

        const string byLocSql = """
            SELECT
                p."LocationId",
                COALESCE(l."Name", 'No Location') AS LocationName,
                COUNT(*) AS ProductCount,
                SUM(p."CurrentStock" * p."UnitPrice") AS TotalValue
            FROM "Products" p
            LEFT JOIN "Locations" l ON UPPER(CAST(l."Id" AS TEXT)) = UPPER(CAST(p."LocationId" AS TEXT))
            WHERE p."IsActive" = 1
            GROUP BY p."LocationId", l."Name" ORDER BY TotalValue DESC
            """;
        var byLocation = (await connection.QueryAsync<LocationValueDto>(byLocSql)).AsList();

        return new InventoryValueDto(
            (decimal)(total.totalvalue ?? 0),
            (decimal)(total.totalcostvalue ?? 0),
            byCategory,
            byLocation);
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalProducts,
                SUM(CASE WHEN "IsActive" = 1 AND "StockStatus" != 'Discontinued' THEN 1 ELSE 0 END) AS ActiveProducts,
                SUM(CASE WHEN "StockStatus" = 'Discontinued' THEN 1 ELSE 0 END) AS DiscontinuedProducts,
                SUM(CASE WHEN "StockStatus" = 'LowStock'     THEN 1 ELSE 0 END) AS LowStockProducts,
                SUM(CASE WHEN "StockStatus" = 'OutOfStock'   THEN 1 ELSE 0 END) AS OutOfStockProducts,
                SUM(CASE WHEN "StockStatus" = 'Overstock'    THEN 1 ELSE 0 END) AS OverstockProducts,
                SUM("CurrentStock" * "UnitPrice") AS TotalInventoryValue
            FROM "Products"
            """;
        var row = await connection.QuerySingleAsync<dynamic>(sql);
        return new InventorySummaryDto(
            (int)(row.totalproducts ?? 0),
            (int)(row.activeproducts ?? 0),
            (int)(row.discontinuedproducts ?? 0),
            (int)(row.lowstockproducts ?? 0),
            (int)(row.outofstockproducts ?? 0),
            (int)(row.overstockproducts ?? 0),
            (decimal)(row.totalinventoryvalue ?? 0));
    }

    public async Task<IReadOnlyList<StockStatusDistributionDto>> GetStockStatusDistributionAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "StockStatus" AS "Status",
                COUNT(*) AS "Count",
                ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM "Products"), 2) AS "Percentage"
            FROM "Products"
            GROUP BY "StockStatus" ORDER BY "Count" DESC
            """;
        return (await connection.QueryAsync<StockStatusDistributionDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<StockTrendDto>> GetStockTrendsAsync(int days, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                strftime('%Y-%m-%d', "MovementDate") AS Date,
                SUM(CASE WHEN "MovementType" IN ('StockIn','Purchase','InitialStock','Return') THEN "Quantity" ELSE 0 END) AS TotalIn,
                SUM(CASE WHEN "MovementType" IN ('StockOut','Sale','Damage','Loss','Expired')  THEN "Quantity" ELSE 0 END) AS TotalOut,
                SUM(CASE WHEN "MovementType" IN ('StockIn','Purchase','InitialStock','Return') THEN "Quantity" ELSE 0 END) -
                SUM(CASE WHEN "MovementType" IN ('StockOut','Sale','Damage','Loss','Expired')  THEN "Quantity" ELSE 0 END) AS NetChange
            FROM "StockMovements"
            WHERE "MovementDate" >= datetime('now', '-{days} days')
            GROUP BY strftime('%Y-%m-%d', "MovementDate")
            ORDER BY Date ASC
            """;
        return (await connection.QueryAsync<StockTrendDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<CategoryDistributionDto>> GetCategoryDistributionAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                "Category",
                COUNT(*) AS "Count",
                ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM "Products"), 2) AS "Percentage"
            FROM "Products"
            GROUP BY "Category" ORDER BY "Count" DESC
            """;
        return (await connection.QueryAsync<CategoryDistributionDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<TurnoverRateDto>> GetTurnoverRateAsync(int topN, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                p."Id" AS ProductId, p."Name" AS ProductName, p."SKU",
                COUNT(sm."Id") AS TotalMovements,
                SUM(CASE WHEN sm."MovementType" IN ('StockOut','Sale','Damage','Loss','Expired') THEN sm."Quantity" ELSE 0 END) AS TotalOut,
                ROUND(
                    CAST(SUM(CASE WHEN sm."MovementType" IN ('StockOut','Sale','Damage','Loss','Expired') THEN sm."Quantity" ELSE 0 END) AS REAL)
                    / NULLIF(AVG(p."CurrentStock"), 0), 4) AS TurnoverRate
            FROM "Products" p
            LEFT JOIN "StockMovements" sm ON UPPER(CAST(sm."ProductId" AS TEXT)) = UPPER(CAST(p."Id" AS TEXT))
            GROUP BY p."Id", p."Name", p."SKU"
            ORDER BY TurnoverRate DESC
            LIMIT {topN}
            """;
        return (await connection.QueryAsync<TurnoverRateDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<ExpiringProductDto>> GetExpiringProductsAsync(int daysAhead, int page, int pageSize, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                "Id" AS ProductId, "Name" AS ProductName, "SKU",
                "CurrentStock", "ExpiryDate",
                CAST(julianday("ExpiryDate") - julianday('now') AS INTEGER) AS DaysUntilExpiry
            FROM "Products"
            WHERE "ExpiryDate" IS NOT NULL
              AND "ExpiryDate" <= datetime('now', '+{daysAhead} days')
              AND "ExpiryDate" >= date('now')
              AND "IsActive" = 1
            ORDER BY "ExpiryDate" ASC
            LIMIT {pageSize} OFFSET {(page - 1) * pageSize}
            """;
        return (await connection.QueryAsync<ExpiringProductDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<LocationCapacityDto>> GetLocationCapacityAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                l."Id" AS LocationId, l."Name" AS LocationName, l."Code" AS LocationCode,
                l."Capacity",
                COALESCE(SUM(p."CurrentStock"), 0) AS CurrentStock,
                ROUND(COALESCE(SUM(p."CurrentStock"), 0) * 100.0 / NULLIF(l."Capacity", 0), 2) AS UtilizationPercent
            FROM "Locations" l
            LEFT JOIN "Products" p ON UPPER(CAST(p."LocationId" AS TEXT)) = UPPER(CAST(l."Id" AS TEXT)) AND p."IsActive" = 1
            WHERE l."IsActive" = 1
            GROUP BY l."Id", l."Name", l."Code", l."Capacity"
            ORDER BY UtilizationPercent DESC
            """;
        return (await connection.QueryAsync<LocationCapacityDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<SupplierPerformanceDto>> GetSupplierPerformanceAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                s."Id" AS SupplierId, s."Name" AS SupplierName, s."Code" AS SupplierCode,
                COUNT(DISTINCT p."Id") AS TotalProducts,
                COUNT(sm."Id") AS TotalPurchases,
                SUM(CASE WHEN sm."MovementType" = 'Purchase' THEN sm."Quantity" * p."CostPrice" ELSE 0 END) AS TotalPurchaseValue
            FROM "Suppliers" s
            LEFT JOIN "Products" p ON UPPER(CAST(p."SupplierId" AS TEXT)) = UPPER(CAST(s."Id" AS TEXT))
            LEFT JOIN "StockMovements" sm ON UPPER(CAST(sm."ProductId" AS TEXT)) = UPPER(CAST(p."Id" AS TEXT))
                AND sm."MovementType" IN ('StockIn','Purchase')
            WHERE s."IsActive" = 1
            GROUP BY s."Id", s."Name", s."Code"
            ORDER BY TotalPurchaseValue DESC
            """;
        return (await connection.QueryAsync<SupplierPerformanceDto>(sql)).AsList();
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int topN, string orderBy, CancellationToken ct = default)
    {
        var orderClause = orderBy.ToLower() switch
        {
            "movement" => "TotalMovements DESC",
            "stock"    => "CurrentStock DESC",
            _          => "TotalValue DESC"
        };

        var sql = $"""
            SELECT
                p."Id" AS ProductId, p."Name" AS ProductName, p."SKU", p."Category",
                p."CurrentStock", p."UnitPrice",
                p."CurrentStock" * p."UnitPrice" AS TotalValue,
                COUNT(sm."Id") AS TotalMovements
            FROM "Products" p
            LEFT JOIN "StockMovements" sm ON UPPER(CAST(sm."ProductId" AS TEXT)) = UPPER(CAST(p."Id" AS TEXT))
            WHERE p."IsActive" = 1
            GROUP BY p."Id", p."Name", p."SKU", p."Category", p."CurrentStock", p."UnitPrice"
            ORDER BY {orderClause}
            LIMIT {topN}
            """;
        return (await connection.QueryAsync<TopProductDto>(sql)).AsList();
    }

    // ── Export helper ─────────────────────────────────────────────────────

    public async Task<byte[]> ExportToJsonAsync(string? module, DateTime? from, DateTime? to)
    {
        object data = module?.ToLower() switch
        {
            "issues" => await GetIssueSummaryAsync(),
            "inventory" => await GetInventorySummaryAsync(),
            "users" => await GetAllUsersWorkloadAsync(),
            _ => new
            {
                Issues = await GetIssueSummaryAsync(),
                Inventory = await GetInventorySummaryAsync(),
                Users = await GetAllUsersWorkloadAsync(),
                GeneratedAt = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }
}
