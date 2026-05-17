using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Application.Mappings;
using IMS.Modular.Modules.Inventory.Application.Queries;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Common;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Inventory.Application.Handlers;

// ── Product Query Handlers ────────────────────────────────────────────────

public sealed class GetProductByIdQueryHandler(
    IProductReadRepository repo,
    ICacheService cache,
    ITenantService tenantService)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        // US-080: include tenant in cache key to prevent cross-tenant cache hits
        var tenant = tenantService.IsMultiTenancyEnabled ? tenantService.TenantId ?? "default" : "global";
        var key = $"inventory-product-{tenant}-{request.Id}";
        var cached = await cache.GetAsync<ProductDto>(key, ct);
        if (cached is not null) return cached;

        var dto = await repo.GetByIdAsync(request.Id, ct);
        var result = dto is null ? null : InventoryMapper.FromReadDto(dto);
        if (result is not null)
            await cache.SetAsync(key, result, TimeSpan.FromSeconds(120), ct);
        return result;
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

public sealed class GetProductsQueryHandler(
    IProductReadRepository repo,
    ICacheService cache,
    ITenantService tenantService)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
{
    public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        // US-080: include tenant in cache key to prevent cross-tenant cache hits
        var tenant = tenantService.IsMultiTenancyEnabled ? tenantService.TenantId ?? "default" : "global";
        var key = $"inventory-products-list-{tenant}-{request.Page}-{request.PageSize}-{request.Category}-{request.StockStatus}-{request.Search}-{request.LocationId}-{request.SupplierId}";
        var cached = await cache.GetAsync<PagedResult<ProductListDto>>(key, ct);
        if (cached is not null) return cached;

        var paged = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Category, request.StockStatus,
            request.LocationId, request.SupplierId,
            request.Search, ct);

        var result = new PagedResult<ProductListDto>(
            paged.Items.Select(InventoryMapper.FromSummaryDto).ToList(),
            paged.TotalCount, paged.Page, paged.PageSize);

        await cache.SetAsync(key, result, TimeSpan.FromSeconds(60), ct);
        return result;
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

// ── US-087: Cursor-based Pagination for Products ─────────────────────────

public sealed class GetProductsCursorQueryHandler(
    InventoryDbContext db,
    ITenantService tenantService)
    : IRequestHandler<GetProductsCursorQuery, CursorPagedResult<ProductListDto>>
{
    public async Task<CursorPagedResult<ProductListDto>> Handle(GetProductsCursorQuery request, CancellationToken ct)
    {
        var query = db.Products
            .IgnoreQueryFilters()
            .WithTenantFilter(tenantService)
            .AsNoTracking()
            .Where(p => p.IsActive)
            .AsQueryable();

        if (request.Category.HasValue)
            query = query.Where(p => p.Category == request.Category.Value);

        if (request.StockStatus.HasValue)
            query = query.Where(p => p.StockStatus == request.StockStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }

        // Apply cursor
        var cursorData = CursorEncoder.Decode(request.Cursor);
        if (cursorData.HasValue)
        {
            var (cursorCreatedAt, cursorId) = cursorData.Value;
            query = query.Where(p =>
                p.CreatedAt < cursorCreatedAt ||
                (p.CreatedAt == cursorCreatedAt && p.Id.CompareTo(cursorId) > 0));
        }

        // Deterministic ordering: CreatedAt DESC, Id ASC
        var ordered = query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);

        var paged = await ordered.ToKeysetPagedAsync(
            request.Cursor,
            request.PageSize,
            p => p.CreatedAt,
            p => p.Id,
            ct);

        var dtos = paged.Items.Select(p => new ProductListDto(
            p.Id, p.Name, p.SKU, p.Category.ToString(),
            p.CurrentStock, p.UnitPrice, p.StockStatus.ToString(),
            p.IsActive, p.CreatedAt)).ToList();

        return new CursorPagedResult<ProductListDto>(dtos, paged.NextCursor, paged.HasMore);
    }
}
