using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.Inventory.Application.Commands;

// ── Product Commands ──────────────────────────────────────────────────────

public record CreateProductCommand(
    string Name,
    string SKU,
    ProductCategory Category,
    int MinimumStockLevel,
    int MaximumStockLevel,
    decimal UnitPrice,
    decimal CostPrice,
    string? Description,
    string? Barcode,
    string Unit,
    string Currency,
    Guid? LocationId,
    Guid? SupplierId,
    DateTime? ExpiryDate) : IRequest<Result<ProductDto>>;

public record UpdateProductCommand(
    Guid Id,
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
    DateTime? ExpiryDate) : IRequest<Result<ProductDto>>;

public record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;

public record AdjustStockCommand(
    Guid ProductId,
    int Quantity,
    StockMovementType MovementType,
    string? Reference,
    string? Notes) : IRequest<Result<ProductDto>>;

public record TransferStockCommand(
    Guid ProductId,
    int Quantity,
    Guid ToLocationId,
    string? Notes) : IRequest<Result<ProductDto>>;

public record DiscontinueProductCommand(Guid Id) : IRequest<Result<ProductDto>>;

public record UpdatePricingCommand(
    Guid Id,
    decimal UnitPrice,
    decimal CostPrice) : IRequest<Result<ProductDto>>;

// ── Stock Movement Commands ───────────────────────────────────────────────

public record CreateStockMovementCommand(
    Guid ProductId,
    StockMovementType MovementType,
    int Quantity,
    Guid? LocationId,
    string? Reference,
    string? Notes) : IRequest<Result<StockMovementDto>>;

public record BulkCreateStockMovementCommand(
    IList<CreateStockMovementCommand> Movements) : IRequest<Result<IList<StockMovementDto>>>;

public record DeleteStockMovementCommand(Guid Id) : IRequest<Result<bool>>;

// ── Supplier Commands ─────────────────────────────────────────────────────

public record CreateSupplierCommand(
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
    string? Notes) : IRequest<Result<SupplierDto>>;

public record UpdateSupplierCommand(
    Guid Id,
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
    string? Notes) : IRequest<Result<SupplierDto>>;

public record DeleteSupplierCommand(Guid Id) : IRequest<Result<bool>>;
public record ActivateSupplierCommand(Guid Id) : IRequest<Result<SupplierDto>>;
public record DeactivateSupplierCommand(Guid Id) : IRequest<Result<SupplierDto>>;

// ── Location Commands ─────────────────────────────────────────────────────

public record CreateLocationCommand(
    string Name,
    string Code,
    LocationType Type,
    int Capacity,
    string? Description,
    Guid? ParentLocationId,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode) : IRequest<Result<LocationDto>>;

public record UpdateLocationCommand(
    Guid Id,
    string Name,
    string? Description,
    LocationType Type,
    int Capacity,
    Guid? ParentLocationId,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode) : IRequest<Result<LocationDto>>;

public record DeleteLocationCommand(Guid Id) : IRequest<Result<bool>>;
public record ActivateLocationCommand(Guid Id) : IRequest<Result<LocationDto>>;
public record DeactivateLocationCommand(Guid Id) : IRequest<Result<LocationDto>>;
public record UpdateLocationCapacityCommand(Guid Id, int Capacity) : IRequest<Result<LocationDto>>;
