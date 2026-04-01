using System.Data;
using Dapper;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.Inventory.Infrastructure;

/// <summary>
/// Dapper read repository for Product — raw SQL, direct DTO projection.
/// </summary>
public class ProductReadRepository(IDbConnection connection) : IProductReadRepository
{
    public async Task<ProductReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Name, SKU, Barcode, Description, Category, CurrentStock,
                   MinimumStockLevel, MaximumStockLevel, UnitPrice, CostPrice,
                   Unit, Currency, LocationId, SupplierId, ExpiryDate,
                   StockStatus, IsActive, CreatedAt, UpdatedAt
            FROM Products
            WHERE Id = @Id
            """;

        return await connection.QuerySingleOrDefaultAsync<ProductReadDto>(sql, new { Id = id.ToString() });
    }

    public async Task<ProductReadDto?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Name, SKU, Barcode, Description, Category, CurrentStock,
                   MinimumStockLevel, MaximumStockLevel, UnitPrice, CostPrice,
                   Unit, Currency, LocationId, SupplierId, ExpiryDate,
                   StockStatus, IsActive, CreatedAt, UpdatedAt
            FROM Products
            WHERE SKU = @SKU
            """;

        return await connection.QuerySingleOrDefaultAsync<ProductReadDto>(sql, new { SKU = sku });
    }

    public async Task<PagedResult<ProductListDto>> GetPagedAsync(
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

        if (category.HasValue)
        {
            whereClauses.Add("Category = @Category");
            parameters.Add("Category", category.Value.ToString());
        }
        if (stockStatus.HasValue)
        {
            whereClauses.Add("StockStatus = @StockStatus");
            parameters.Add("StockStatus", stockStatus.Value.ToString());
        }
        if (locationId.HasValue)
        {
            whereClauses.Add("LocationId = @LocationId");
            parameters.Add("LocationId", locationId.Value.ToString());
        }
        if (supplierId.HasValue)
        {
            whereClauses.Add("SupplierId = @SupplierId");
            parameters.Add("SupplierId", supplierId.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(Name LIKE @Search OR SKU LIKE @Search OR Description LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"""
            SELECT Id, Name, SKU, Category, CurrentStock, UnitPrice, StockStatus, IsActive, CreatedAt
            FROM Products
            {whereClause}
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM Products {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<ProductListDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<ProductListDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Dapper read repository for StockMovement.
/// </summary>
public class StockMovementReadRepository(IDbConnection connection) : IStockMovementReadRepository
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

        if (productId.HasValue)
        {
            whereClauses.Add("sm.ProductId = @ProductId");
            parameters.Add("ProductId", productId.Value.ToString());
        }
        if (movementType.HasValue)
        {
            whereClauses.Add("sm.MovementType = @MovementType");
            parameters.Add("MovementType", movementType.Value.ToString());
        }
        if (fromDate.HasValue)
        {
            whereClauses.Add("sm.MovementDate >= @FromDate");
            parameters.Add("FromDate", fromDate.Value.ToString("yyyy-MM-dd"));
        }
        if (toDate.HasValue)
        {
            whereClauses.Add("sm.MovementDate <= @ToDate");
            parameters.Add("ToDate", toDate.Value.ToString("yyyy-MM-dd"));
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"""
            SELECT sm.Id, sm.ProductId, p.Name AS ProductName, p.SKU AS ProductSKU,
                   sm.MovementType, sm.Quantity, sm.LocationId, l.Name AS LocationName,
                   sm.Reference, sm.Notes, sm.MovementDate
            FROM StockMovements sm
            LEFT JOIN Products p ON p.Id = sm.ProductId
            LEFT JOIN Locations l ON l.Id = sm.LocationId
            {whereClause}
            ORDER BY sm.MovementDate DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM StockMovements sm {whereClause};
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
public class SupplierReadRepository(IDbConnection connection) : ISupplierReadRepository
{
    public async Task<SupplierReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Name, Code, ContactPerson, Email, Phone, Address, City, State,
                   Country, PostalCode, TaxId, CreditLimit, PaymentTermsDays,
                   IsActive, Notes, CreatedAt, UpdatedAt
            FROM Suppliers
            WHERE Id = @Id
            """;

        return await connection.QuerySingleOrDefaultAsync<SupplierReadDto>(sql, new { Id = id.ToString() });
    }

    public async Task<PagedResult<SupplierListDto>> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (isActive.HasValue)
        {
            whereClauses.Add("IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(Name LIKE @Search OR Code LIKE @Search OR ContactPerson LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"""
            SELECT Id, Name, Code, ContactPerson, Email, IsActive, CreatedAt
            FROM Suppliers
            {whereClause}
            ORDER BY Name ASC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM Suppliers {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<SupplierListDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<SupplierListDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Dapper read repository for Location.
/// </summary>
public class LocationReadRepository(IDbConnection connection) : ILocationReadRepository
{
    public async Task<LocationReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Name, Code, Type, Capacity, Description, ParentLocationId,
                   Address, City, State, Country, PostalCode, IsActive, CreatedAt, UpdatedAt
            FROM Locations
            WHERE Id = @Id
            """;

        return await connection.QuerySingleOrDefaultAsync<LocationReadDto>(sql, new { Id = id.ToString() });
    }

    public async Task<PagedResult<LocationListDto>> GetPagedAsync(
        int page,
        int pageSize,
        Domain.Enums.LocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (type.HasValue)
        {
            whereClauses.Add("Type = @Type");
            parameters.Add("Type", type.Value.ToString());
        }
        if (isActive.HasValue)
        {
            whereClauses.Add("IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClauses.Add("(Name LIKE @Search OR Code LIKE @Search OR Description LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"""
            SELECT Id, Name, Code, Type, Capacity, IsActive, CreatedAt
            FROM Locations
            {whereClause}
            ORDER BY Name ASC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM Locations {whereClause};
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<LocationListDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<LocationListDto>(items, totalCount, page, pageSize);
    }
}
