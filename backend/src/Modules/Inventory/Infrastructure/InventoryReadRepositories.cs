using System.Data;
using Dapper;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Shared.Domain;
using IMS.Modular.Shared.MultiTenancy;

namespace IMS.Modular.Modules.Inventory.Infrastructure;

/// <summary>
/// Helper to normalise Guid to uppercase string for SQLite TEXT comparisons.
/// EF Core stores GUIDs as uppercase TEXT; C# Guid.ToString() is lowercase.
/// </summary>
file static class GuidHelper
{
    internal static string Up(Guid id) => id.ToString().ToUpperInvariant();
    internal static string? Up(Guid? id) => id.HasValue ? id.Value.ToString().ToUpperInvariant() : null;
}

/// <summary>
/// Helper to build tenant WHERE clause for Dapper queries.
/// When multi-tenancy is active, restricts to current tenant (or NULL = shared records).
/// </summary>
file static class TenantFilter
{
    internal static (string clause, DynamicParameters p) Build(ITenantService tenant, DynamicParameters? existing = null)
    {
        var p = existing ?? new DynamicParameters();
        if (tenant.IsMultiTenancyEnabled && tenant.TenantId is not null)
        {
            p.Add("TenantId", tenant.TenantId);
            return ("(\"TenantId\" IS NULL OR \"TenantId\" = @TenantId)", p);
        }
        return ("1=1", p);
    }
}

/// <summary>
/// Dapper read repository for Product — raw SQL, direct DTO projection.
/// US-080: Applies tenant filter on all queries when multi-tenancy is enabled.
/// </summary>
public class ProductReadRepository(IDbConnection connection, ITenantService tenantService) : IProductReadRepository
{
    public async Task<ProductReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var (tenantClause, p) = TenantFilter.Build(tenantService);
        p.Add("Id", GuidHelper.Up(id));
        var sql = $"""
            SELECT "Id", "Name", "SKU", "Barcode", "Description", "Category", "CurrentStock",
                   "MinimumStockLevel", "MaximumStockLevel", "UnitPrice", "CostPrice",
                   "Unit", "Currency", "LocationId", "SupplierId", "ExpiryDate",
                   "StockStatus", "IsActive", "CreatedAt", "UpdatedAt"
            FROM "Products"
            WHERE UPPER(CAST("Id" AS TEXT)) = @Id AND {tenantClause}
            """;

        return await connection.QuerySingleOrDefaultAsync<ProductReadDto>(sql, p);
    }

    public async Task<ProductReadDto?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var (tenantClause, p) = TenantFilter.Build(tenantService);
        p.Add("SKU", sku);
        var sql = $"""
            SELECT "Id", "Name", "SKU", "Barcode", "Description", "Category", "CurrentStock",
                   "MinimumStockLevel", "MaximumStockLevel", "UnitPrice", "CostPrice",
                   "Unit", "Currency", "LocationId", "SupplierId", "ExpiryDate",
                   "StockStatus", "IsActive", "CreatedAt", "UpdatedAt"
            FROM "Products"
            WHERE "SKU" = @SKU AND {tenantClause}
            """;

        return await connection.QuerySingleOrDefaultAsync<ProductReadDto>(sql, p);
    }

    public async Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
        int page,
        int pageSize,
        Domain.Enums.ProductCategory? category = null,
        Domain.Enums.StockStatus? stockStatus = null,
        Guid? locationId = null,
        Guid? supplierId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        // US-080: tenant isolation
        var (tenantClause, _) = TenantFilter.Build(tenantService, parameters);
        whereClauses.Add(tenantClause);

        if (category.HasValue)
        {
            whereClauses.Add("\"Category\" = @Category");
            parameters.Add("Category", category.Value.ToString());
        }
        if (stockStatus.HasValue)
        {
            whereClauses.Add("\"StockStatus\" = @StockStatus");
            parameters.Add("StockStatus", stockStatus.Value.ToString());
        }
        if (locationId.HasValue)
        {
            whereClauses.Add("UPPER(CAST(\"LocationId\" AS TEXT)) = @LocationId");
            parameters.Add("LocationId", GuidHelper.Up(locationId));
        }
        if (supplierId.HasValue)
        {
            whereClauses.Add("UPPER(CAST(\"SupplierId\" AS TEXT)) = @SupplierId");
            parameters.Add("SupplierId", GuidHelper.Up(supplierId));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(UPPER(\"Name\") LIKE UPPER(@Search) OR UPPER(\"SKU\") LIKE UPPER(@Search) OR UPPER(\"Description\") LIKE UPPER(@Search))");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT "Id", "Name", "SKU", "Category", "CurrentStock", "UnitPrice", "StockStatus", "IsActive", "CreatedAt"
            FROM "Products"
            {whereClause}
            ORDER BY "CreatedAt" DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM "Products" {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<ProductSummaryDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<ProductSummaryDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Dapper read repository for StockMovement.
/// </summary>
public class StockMovementReadRepository(IDbConnection connection, ITenantService tenantService) : IStockMovementReadRepository
{
    public async Task<PagedResult<StockMovementReadDto>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? productId = null,
        Domain.Enums.StockMovementType? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        // US-080: tenant isolation (applied to StockMovements table alias sm)
        if (tenantService.IsMultiTenancyEnabled && tenantService.TenantId is not null)
        {
            parameters.Add("TenantId", tenantService.TenantId);
            whereClauses.Add("(sm.\"TenantId\" IS NULL OR sm.\"TenantId\" = @TenantId)");
        }

        if (productId.HasValue)
        {
            whereClauses.Add("UPPER(CAST(sm.\"ProductId\" AS TEXT)) = @ProductId");
            parameters.Add("ProductId", GuidHelper.Up(productId));
        }
        if (movementType.HasValue)
        {
            whereClauses.Add("sm.\"MovementType\" = @MovementType");
            parameters.Add("MovementType", movementType.Value.ToString());
        }
        if (fromDate.HasValue)
        {
            whereClauses.Add("sm.\"MovementDate\" >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }
        if (toDate.HasValue)
        {
            whereClauses.Add("sm.\"MovementDate\" <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"""
            SELECT sm."Id", sm."ProductId", p."Name" AS "ProductName", p."SKU" AS "ProductSKU",
                   sm."MovementType", sm."Quantity", sm."LocationId", l."Name" AS "LocationName",
                   sm."Reference", sm."Notes", sm."MovementDate"
            FROM "StockMovements" sm
            LEFT JOIN "Products" p ON UPPER(CAST(p."Id" AS TEXT)) = UPPER(CAST(sm."ProductId" AS TEXT))
            LEFT JOIN "Locations" l ON UPPER(CAST(l."Id" AS TEXT)) = UPPER(CAST(sm."LocationId" AS TEXT))
            {whereClause}
            ORDER BY sm."MovementDate" DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM "StockMovements" sm {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<StockMovementReadDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<StockMovementReadDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Dapper read repository for Supplier.
/// </summary>
public class SupplierReadRepository(IDbConnection connection, ITenantService tenantService) : ISupplierReadRepository
{
    public async Task<SupplierReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var (tenantClause, p) = TenantFilter.Build(tenantService);
        p.Add("Id", GuidHelper.Up(id));
        var sql = $"""
            SELECT "Id", "Name", "Code", "ContactPerson", "Email", "Phone", "Address", "City", "State",
                   "Country", "PostalCode", "TaxId", "CreditLimit", "PaymentTermsDays",
                   "IsActive", "Notes", "CreatedAt", "UpdatedAt"
            FROM "Suppliers"
            WHERE UPPER(CAST("Id" AS TEXT)) = @Id AND {tenantClause}
            """;

        return await connection.QuerySingleOrDefaultAsync<SupplierReadDto>(sql, p);
    }

    public async Task<PagedResult<SupplierSummaryDto>> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        var (tenantClause, _) = TenantFilter.Build(tenantService, parameters);
        whereClauses.Add(tenantClause);

        if (isActive.HasValue)
        {
            whereClauses.Add("\"IsActive\" = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(UPPER(\"Name\") LIKE UPPER(@Search) OR UPPER(\"Code\") LIKE UPPER(@Search) OR UPPER(\"ContactPerson\") LIKE UPPER(@Search))");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT "Id", "Name", "Code", "ContactPerson", "Email", "IsActive", "CreatedAt"
            FROM "Suppliers"
            {whereClause}
            ORDER BY "Name" ASC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM "Suppliers" {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<SupplierSummaryDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<SupplierSummaryDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Dapper read repository for Location.
/// </summary>
public class LocationReadRepository(IDbConnection connection, ITenantService tenantService) : ILocationReadRepository
{
    public async Task<LocationReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var (tenantClause, p) = TenantFilter.Build(tenantService);
        p.Add("Id", GuidHelper.Up(id));
        var sql = $"""
            SELECT "Id", "Name", "Code", "Type", "Capacity", "Description", "ParentLocationId",
                   "Address", "City", "State", "Country", "PostalCode", "IsActive", "CreatedAt", "UpdatedAt"
            FROM "Locations"
            WHERE UPPER(CAST("Id" AS TEXT)) = @Id AND {tenantClause}
            """;

        return await connection.QuerySingleOrDefaultAsync<LocationReadDto>(sql, p);
    }

    public async Task<PagedResult<LocationSummaryDto>> GetPagedAsync(
        int page,
        int pageSize,
        Domain.Enums.LocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        var (tenantClause, _) = TenantFilter.Build(tenantService, parameters);
        whereClauses.Add(tenantClause);

        if (type.HasValue)
        {
            whereClauses.Add("\"Type\" = @Type");
            parameters.Add("Type", type.Value.ToString());
        }
        if (isActive.HasValue)
        {
            whereClauses.Add("\"IsActive\" = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(UPPER(\"Name\") LIKE UPPER(@Search) OR UPPER(\"Code\") LIKE UPPER(@Search) OR UPPER(\"Description\") LIKE UPPER(@Search))");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT "Id", "Name", "Code", "Type", "Capacity", "IsActive", "CreatedAt"
            FROM "Locations"
            {whereClause}
            ORDER BY "Name" ASC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM "Locations" {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<LocationSummaryDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<LocationSummaryDto>(items, totalCount, page, pageSize);
    }
}
