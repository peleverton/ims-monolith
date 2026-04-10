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
    Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
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
    Task<PagedResult<SupplierSummaryDto>> GetPagedAsync(
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
    Task<PagedResult<LocationSummaryDto>> GetPagedAsync(
        int page,
        int pageSize,
        LocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default);
}

// ── Read DTOs (projected directly by Dapper) ─────────────────────────
// Note: Uses classes with property setters — required for Dapper to map
// SQLite's native types (bool as Int64, Guid as string) correctly.

public sealed class ProductReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string SKU { get; set; } = null!;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public Guid? LocationId { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string StockStatus { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class ProductSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string SKU { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int CurrentStock { get; set; }
    public decimal UnitPrice { get; set; }
    public string StockStatus { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class StockMovementReadDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSKU { get; set; }
    public string MovementType { get; set; } = null!;
    public int Quantity { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; }
}

public sealed class SupplierReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxId { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class SupplierSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class LocationReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public Guid? ParentLocationId { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class LocationSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
