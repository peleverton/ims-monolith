using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.Inventory.Application.Queries;

// ── Product Queries ───────────────────────────────────────────────────────

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
public record GetProductBySkuQuery(string SKU) : IRequest<ProductDto?>;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 10,
    ProductCategory? Category = null,
    StockStatus? StockStatus = null,
    Guid? LocationId = null,
    Guid? SupplierId = null,
    string? Search = null) : IRequest<PagedResult<ProductListDto>>;

// ── Stock Movement Queries ────────────────────────────────────────────────

public record GetStockMovementsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ProductId = null,
    StockMovementType? MovementType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PagedResult<StockMovementDto>>;

// ── Supplier Queries ──────────────────────────────────────────────────────

public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierDto?>;

public record GetSuppliersQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null,
    string? Search = null) : IRequest<PagedResult<SupplierListDto>>;

// ── Location Queries ──────────────────────────────────────────────────────

public record GetLocationByIdQuery(Guid Id) : IRequest<LocationDto?>;

public record GetLocationsQuery(
    int Page = 1,
    int PageSize = 20,
    LocationType? Type = null,
    bool? IsActive = null,
    string? Search = null) : IRequest<PagedResult<LocationListDto>>;
