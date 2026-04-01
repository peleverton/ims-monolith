using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.Inventory.Domain;

/// <summary>
/// Read repository for Product — Dapper (raw SQL, direct DTO projection).
/// </summary>
public interface IProductReadRepository
{
    Task<ProductReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductReadDto?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<PagedResult<ProductListDto>> GetPagedAsync(
        int page,
        int pageSize,
        ProductCategory? category = null,
        StockStatus? stockStatus = null,
        Guid? locationId = null,
        Guid? supplierId = null,
        string? search = null,
        CancellationToken ct = default);
}

/// <summary>
/// Read repository for StockMovement — Dapper.
/// </summary>
public interface IStockMovementReadRepository
{
    Task<PagedResult<StockMovementReadDto>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? productId = null,
        StockMovementType? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
}

/// <summary>
/// Read repository for Supplier — Dapper.
/// </summary>
public interface ISupplierReadRepository
{
    Task<SupplierReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SupplierListDto>> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default);
}

/// <summary>
/// Read repository for Location — Dapper.
/// </summary>
public interface ILocationReadRepository
{
    Task<LocationReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LocationListDto>> GetPagedAsync(
        int page,
        int pageSize,
        LocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default);
}

// ── Read DTOs (projected directly by Dapper) ──────────────────────────

public sealed record ProductReadDto(
    Guid Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    string Category,
    int CurrentStock,
    int MinimumStockLevel,
    int MaximumStockLevel,
    decimal UnitPrice,
    decimal CostPrice,
    string Unit,
    string Currency,
    Guid? LocationId,
    Guid? SupplierId,
    DateTime? ExpiryDate,
    string StockStatus,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ProductListDto(
    Guid Id,
    string Name,
    string SKU,
    string Category,
    int CurrentStock,
    decimal UnitPrice,
    string StockStatus,
    bool IsActive,
    DateTime CreatedAt);

public sealed record StockMovementReadDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    string? ProductSKU,
    string MovementType,
    int Quantity,
    Guid? LocationId,
    string? LocationName,
    string? Reference,
    string? Notes,
    DateTime MovementDate);

public sealed record SupplierReadDto(
    Guid Id,
    string Name,
    string Code,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string? TaxId,
    decimal CreditLimit,
    int PaymentTermsDays,
    bool IsActive,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SupplierListDto(
    Guid Id,
    string Name,
    string Code,
    string? ContactPerson,
    string? Email,
    bool IsActive,
    DateTime CreatedAt);

public sealed record LocationReadDto(
    Guid Id,
    string Name,
    string Code,
    string Type,
    int Capacity,
    string? Description,
    Guid? ParentLocationId,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record LocationListDto(
    Guid Id,
    string Name,
    string Code,
    string Type,
    int Capacity,
    bool IsActive,
    DateTime CreatedAt);
