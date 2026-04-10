using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Modules.Inventory.Application.DTOs;

// ── Product DTOs ─────────────────────────────────────────────────────────

public record ProductDto(
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

public record ProductListDto(
    Guid Id,
    string Name,
    string SKU,
    string Category,
    int CurrentStock,
    decimal UnitPrice,
    string StockStatus,
    bool IsActive,
    DateTime CreatedAt);

public record CreateProductRequest(
    string Name,
    string SKU,
    ProductCategory Category,
    int MinimumStockLevel,
    int MaximumStockLevel,
    decimal UnitPrice,
    decimal CostPrice,
    string? Description = null,
    string? Barcode = null,
    string Unit = "un",
    string Currency = "BRL",
    Guid? LocationId = null,
    Guid? SupplierId = null,
    DateTime? ExpiryDate = null);

public record UpdateProductRequest(
    string Name,
    string? Description,
    ProductCategory Category,
    int MinimumStockLevel,
    int MaximumStockLevel,
    string? Barcode,
    string Unit,
    string Currency,
    Guid? LocationId,
    Guid? SupplierId,
    DateTime? ExpiryDate);

public record UpdatePricingRequest(
    decimal UnitPrice,
    decimal CostPrice);

public record AdjustStockRequest(
    int Quantity,
    StockMovementType MovementType,
    string? Reference = null,
    string? Notes = null);

public record TransferStockRequest(
    int Quantity,
    Guid ToLocationId,
    string? Notes = null);

// ── Stock Movement DTOs ───────────────────────────────────────────────────

public record StockMovementDto(
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

public record CreateStockMovementRequest(
    Guid ProductId,
    StockMovementType MovementType,
    int Quantity,
    Guid? LocationId = null,
    string? Reference = null,
    string? Notes = null);

public record BulkCreateStockMovementRequest(
    IList<CreateStockMovementRequest> Movements);

// ── Supplier DTOs ─────────────────────────────────────────────────────────

public record SupplierDto(
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

public record SupplierListDto(
    Guid Id,
    string Name,
    string Code,
    string? ContactPerson,
    string? Email,
    bool IsActive,
    DateTime CreatedAt);

public record CreateSupplierRequest(
    string Name,
    string Code,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    string? PostalCode = null,
    string? TaxId = null,
    decimal CreditLimit = 0,
    int PaymentTermsDays = 30,
    string? Notes = null);

public record UpdateSupplierRequest(
    string Name,
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
    string? Notes);

// ── Location DTOs ─────────────────────────────────────────────────────────

public record LocationDto(
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

public record LocationListDto(
    Guid Id,
    string Name,
    string Code,
    string Type,
    int Capacity,
    bool IsActive,
    DateTime CreatedAt);

public record CreateLocationRequest(
    string Name,
    string Code,
    LocationType Type,
    int Capacity,
    string? Description = null,
    Guid? ParentLocationId = null,
    string? Address = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    string? PostalCode = null);

public record UpdateLocationRequest(
    string Name,
    string? Description,
    LocationType Type,
    int Capacity,
    Guid? ParentLocationId,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode);

public record UpdateCapacityRequest(int Capacity);
