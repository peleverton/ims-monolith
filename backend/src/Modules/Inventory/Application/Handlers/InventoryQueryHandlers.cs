using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Application.Mappings;
using IMS.Modular.Modules.Inventory.Application.Queries;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.Inventory.Application.Handlers;

// ── Product Query Handlers ────────────────────────────────────────────────

public sealed class GetProductByIdQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var dto = await repo.GetByIdAsync(request.Id, ct);
        return dto is null ? null : InventoryMapper.FromReadDto(dto);
    }
}

public sealed class GetProductBySkuQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetProductBySkuQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductBySkuQuery request, CancellationToken ct)
    {
        var dto = await repo.GetBySkuAsync(request.SKU, ct);
        return dto is null ? null : InventoryMapper.FromReadDto(dto);
    }
}

public sealed class GetProductsQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
{
    public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var paged = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Category, request.StockStatus,
            request.LocationId, request.SupplierId,
            request.Search, ct);

        return new PagedResult<ProductListDto>(
            paged.Items.Select(InventoryMapper.FromSummaryDto).ToList(),
            paged.TotalCount, paged.Page, paged.PageSize);
    }
}

// ── Stock Movement Query Handlers ─────────────────────────────────────────

public sealed class GetStockMovementsQueryHandler(IStockMovementReadRepository repo)
    : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    public async Task<PagedResult<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken ct)
    {
        var paged = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.ProductId, request.MovementType,
            request.FromDate, request.ToDate, ct);

        return new PagedResult<StockMovementDto>(
            paged.Items.Select(InventoryMapper.FromReadDto).ToList(),
            paged.TotalCount, paged.Page, paged.PageSize);
    }
}

// ── Supplier Query Handlers ───────────────────────────────────────────────

public sealed class GetSupplierByIdQueryHandler(ISupplierReadRepository repo)
    : IRequestHandler<GetSupplierByIdQuery, SupplierDto?>
{
    public async Task<SupplierDto?> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var dto = await repo.GetByIdAsync(request.Id, ct);
        return dto is null ? null : InventoryMapper.FromReadDto(dto);
    }
}

public sealed class GetSuppliersQueryHandler(ISupplierReadRepository repo)
    : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierListDto>>
{
    public async Task<PagedResult<SupplierListDto>> Handle(GetSuppliersQuery request, CancellationToken ct)
    {
        var paged = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.IsActive, request.Search, ct);

        return new PagedResult<SupplierListDto>(
            paged.Items.Select(InventoryMapper.FromSummaryDto).ToList(),
            paged.TotalCount, paged.Page, paged.PageSize);
    }
}

// ── Location Query Handlers ───────────────────────────────────────────────

public sealed class GetLocationByIdQueryHandler(ILocationReadRepository repo)
    : IRequestHandler<GetLocationByIdQuery, LocationDto?>
{
    public async Task<LocationDto?> Handle(GetLocationByIdQuery request, CancellationToken ct)
    {
        var dto = await repo.GetByIdAsync(request.Id, ct);
        return dto is null ? null : InventoryMapper.FromReadDto(dto);
    }
}

public sealed class GetLocationsQueryHandler(ILocationReadRepository repo)
    : IRequestHandler<GetLocationsQuery, PagedResult<LocationListDto>>
{
    public async Task<PagedResult<LocationListDto>> Handle(GetLocationsQuery request, CancellationToken ct)
    {
        var paged = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Type, request.IsActive,
            request.Search, ct);

        return new PagedResult<LocationListDto>(
            paged.Items.Select(InventoryMapper.FromSummaryDto).ToList(),
            paged.TotalCount, paged.Page, paged.PageSize);
    }
}
