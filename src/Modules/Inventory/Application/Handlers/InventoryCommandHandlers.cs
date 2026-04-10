using IMS.Modular.Modules.Inventory.Application.Commands;
using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Application.Mappings;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Inventory.Application.Handlers;

// ── Product Command Handlers ───────────────────────────────────────────────

public sealed class CreateProductCommandHandler(
    IProductRepository repo,
    ILogger<CreateProductCommandHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetBySkuAsync(cmd.SKU, ct);
        if (existing is not null)
            return Result<ProductDto>.Conflict($"A product with SKU '{cmd.SKU}' already exists.");

        var product = new Product(
            cmd.Name, cmd.SKU, cmd.Category,
            cmd.MinimumStockLevel, cmd.MaximumStockLevel,
            cmd.UnitPrice, cmd.CostPrice,
            cmd.Description, cmd.Barcode, cmd.Unit, cmd.Currency);

        if (cmd.LocationId.HasValue) product.SetLocation(cmd.LocationId);
        if (cmd.SupplierId.HasValue) product.SetSupplier(cmd.SupplierId);

        await repo.AddAsync(product, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Product created: {ProductId} SKU={SKU}", product.Id, product.SKU);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

public sealed class UpdateProductCommandHandler(
    IProductRepository repo,
    ILogger<UpdateProductCommandHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.Id, ct);
        if (product is null) return Result<ProductDto>.NotFound($"Product {cmd.Id} not found.");

        product.Update(cmd.Name, cmd.Description, cmd.Category,
            cmd.MinimumStockLevel, cmd.MaximumStockLevel,
            cmd.Barcode, cmd.Unit, cmd.Currency, cmd.ExpiryDate);
        product.SetLocation(cmd.LocationId);
        product.SetSupplier(cmd.SupplierId);

        repo.Update(product);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Product updated: {ProductId}", cmd.Id);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

public sealed class DeleteProductCommandHandler(
    IProductRepository repo,
    ILogger<DeleteProductCommandHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.Id, ct);
        if (product is null) return Result<bool>.NotFound($"Product {cmd.Id} not found.");

        repo.Remove(product);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Product deleted: {ProductId}", cmd.Id);
        return Result<bool>.Success(true);
    }
}

public sealed class AdjustStockCommandHandler(
    IProductRepository productRepo,
    IStockMovementRepository movementRepo,
    ILogger<AdjustStockCommandHandler> logger)
    : IRequestHandler<AdjustStockCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result<ProductDto>.NotFound($"Product {cmd.ProductId} not found.");

        product.AdjustStock(cmd.Quantity, cmd.MovementType);

        var movement = new StockMovement(
            product.Id, cmd.MovementType, cmd.Quantity,
            product.LocationId, cmd.Reference, cmd.Notes);

        productRepo.Update(product);
        await movementRepo.AddAsync(movement, ct);
        await productRepo.SaveChangesAsync(ct);

        logger.LogInformation("Stock adjusted: Product={ProductId} Qty={Qty} Type={Type}",
            cmd.ProductId, cmd.Quantity, cmd.MovementType);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

public sealed class TransferStockCommandHandler(
    IProductRepository productRepo,
    IStockMovementRepository movementRepo,
    ILogger<TransferStockCommandHandler> logger)
    : IRequestHandler<TransferStockCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(TransferStockCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result<ProductDto>.NotFound($"Product {cmd.ProductId} not found.");

        var fromLocationId = product.LocationId;
        product.TransferStock(cmd.Quantity, fromLocationId, cmd.ToLocationId);

        var movement = new StockMovement(
            product.Id, Domain.Enums.StockMovementType.Transfer, cmd.Quantity,
            cmd.ToLocationId, notes: cmd.Notes);

        productRepo.Update(product);
        await movementRepo.AddAsync(movement, ct);
        await productRepo.SaveChangesAsync(ct);

        logger.LogInformation("Stock transferred: Product={ProductId} To={ToLocation}", cmd.ProductId, cmd.ToLocationId);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

public sealed class DiscontinueProductCommandHandler(
    IProductRepository repo,
    ILogger<DiscontinueProductCommandHandler> logger)
    : IRequestHandler<DiscontinueProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(DiscontinueProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.Id, ct);
        if (product is null) return Result<ProductDto>.NotFound($"Product {cmd.Id} not found.");

        product.Discontinue();
        repo.Update(product);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Product discontinued: {ProductId}", cmd.Id);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

public sealed class UpdatePricingCommandHandler(
    IProductRepository repo,
    ILogger<UpdatePricingCommandHandler> logger)
    : IRequestHandler<UpdatePricingCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdatePricingCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.Id, ct);
        if (product is null) return Result<ProductDto>.NotFound($"Product {cmd.Id} not found.");

        product.UpdatePricing(cmd.UnitPrice, cmd.CostPrice);
        repo.Update(product);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Pricing updated: Product={ProductId} UnitPrice={Price}", cmd.Id, cmd.UnitPrice);
        return Result<ProductDto>.Success(InventoryMapper.ToDto(product));
    }
}

// ── Stock Movement Command Handlers ───────────────────────────────────────

public sealed class CreateStockMovementCommandHandler(
    IProductRepository productRepo,
    IStockMovementRepository movementRepo,
    ILogger<CreateStockMovementCommandHandler> logger)
    : IRequestHandler<CreateStockMovementCommand, Result<StockMovementDto>>
{
    public async Task<Result<StockMovementDto>> Handle(CreateStockMovementCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result<StockMovementDto>.NotFound($"Product {cmd.ProductId} not found.");

        var movement = new StockMovement(
            cmd.ProductId, cmd.MovementType, cmd.Quantity, cmd.LocationId, cmd.Reference, cmd.Notes);

        product.AdjustStock(cmd.Quantity, cmd.MovementType);
        productRepo.Update(product);
        await movementRepo.AddAsync(movement, ct);
        await productRepo.SaveChangesAsync(ct);

        logger.LogInformation("StockMovement created: {MovementId}", movement.Id);
        return Result<StockMovementDto>.Success(InventoryMapper.ToDto(movement, product.Name, product.SKU));
    }
}

public sealed class BulkCreateStockMovementCommandHandler(
    IProductRepository productRepo,
    IStockMovementRepository movementRepo,
    ILogger<BulkCreateStockMovementCommandHandler> logger)
    : IRequestHandler<BulkCreateStockMovementCommand, Result<IList<StockMovementDto>>>
{
    public async Task<Result<IList<StockMovementDto>>> Handle(BulkCreateStockMovementCommand cmd, CancellationToken ct)
    {
        var results = new List<StockMovementDto>();
        var movements = new List<StockMovement>();

        foreach (var m in cmd.Movements)
        {
            var product = await productRepo.GetByIdAsync(m.ProductId, ct);
            if (product is null) continue;

            var movement = new StockMovement(m.ProductId, m.MovementType, m.Quantity, m.LocationId, m.Reference, m.Notes);
            product.AdjustStock(m.Quantity, m.MovementType);
            productRepo.Update(product);
            movements.Add(movement);
            results.Add(InventoryMapper.ToDto(movement, product.Name, product.SKU));
        }

        await movementRepo.AddRangeAsync(movements, ct);
        await productRepo.SaveChangesAsync(ct);

        logger.LogInformation("Bulk StockMovements created: {Count}", movements.Count);
        return Result<IList<StockMovementDto>>.Success(results);
    }
}

public sealed class DeleteStockMovementCommandHandler(
    IStockMovementRepository repo,
    ILogger<DeleteStockMovementCommandHandler> logger)
    : IRequestHandler<DeleteStockMovementCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteStockMovementCommand cmd, CancellationToken ct)
    {
        var movement = await repo.GetByIdAsync(cmd.Id, ct);
        if (movement is null) return Result<bool>.NotFound($"StockMovement {cmd.Id} not found.");

        repo.Remove(movement);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("StockMovement deleted: {MovementId}", cmd.Id);
        return Result<bool>.Success(true);
    }
}

// ── Supplier Command Handlers ─────────────────────────────────────────────

public sealed class CreateSupplierCommandHandler(
    ISupplierRepository repo,
    ILogger<CreateSupplierCommandHandler> logger)
    : IRequestHandler<CreateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(CreateSupplierCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetByCodeAsync(cmd.Code, ct);
        if (existing is not null)
            return Result<SupplierDto>.Conflict($"A supplier with code '{cmd.Code}' already exists.");

        var supplier = new Supplier(cmd.Name, cmd.Code, cmd.ContactPerson, cmd.Email, cmd.Phone);
        supplier.Update(cmd.Name, cmd.ContactPerson, cmd.Email, cmd.Phone,
            cmd.Address, cmd.City, cmd.State, cmd.Country, cmd.PostalCode,
            cmd.TaxId, cmd.CreditLimit, cmd.PaymentTermsDays, cmd.Notes);

        await repo.AddAsync(supplier, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Supplier created: {SupplierId} Code={Code}", supplier.Id, supplier.Code);
        return Result<SupplierDto>.Success(InventoryMapper.ToDto(supplier));
    }
}

public sealed class UpdateSupplierCommandHandler(
    ISupplierRepository repo,
    ILogger<UpdateSupplierCommandHandler> logger)
    : IRequestHandler<UpdateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(UpdateSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await repo.GetByIdAsync(cmd.Id, ct);
        if (supplier is null) return Result<SupplierDto>.NotFound($"Supplier {cmd.Id} not found.");

        supplier.Update(cmd.Name, cmd.ContactPerson, cmd.Email, cmd.Phone,
            cmd.Address, cmd.City, cmd.State, cmd.Country, cmd.PostalCode,
            cmd.TaxId, cmd.CreditLimit, cmd.PaymentTermsDays, cmd.Notes);
        repo.Update(supplier);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Supplier updated: {SupplierId}", cmd.Id);
        return Result<SupplierDto>.Success(InventoryMapper.ToDto(supplier));
    }
}

public sealed class DeleteSupplierCommandHandler(
    ISupplierRepository repo,
    ILogger<DeleteSupplierCommandHandler> logger)
    : IRequestHandler<DeleteSupplierCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await repo.GetByIdAsync(cmd.Id, ct);
        if (supplier is null) return Result<bool>.NotFound($"Supplier {cmd.Id} not found.");

        repo.Remove(supplier);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Supplier deleted: {SupplierId}", cmd.Id);
        return Result<bool>.Success(true);
    }
}

public sealed class ActivateSupplierCommandHandler(
    ISupplierRepository repo)
    : IRequestHandler<ActivateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(ActivateSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await repo.GetByIdAsync(cmd.Id, ct);
        if (supplier is null) return Result<SupplierDto>.NotFound($"Supplier {cmd.Id} not found.");
        supplier.Activate();
        repo.Update(supplier);
        await repo.SaveChangesAsync(ct);
        return Result<SupplierDto>.Success(InventoryMapper.ToDto(supplier));
    }
}

public sealed class DeactivateSupplierCommandHandler(
    ISupplierRepository repo)
    : IRequestHandler<DeactivateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(DeactivateSupplierCommand cmd, CancellationToken ct)
    {
        var supplier = await repo.GetByIdAsync(cmd.Id, ct);
        if (supplier is null) return Result<SupplierDto>.NotFound($"Supplier {cmd.Id} not found.");
        supplier.Deactivate();
        repo.Update(supplier);
        await repo.SaveChangesAsync(ct);
        return Result<SupplierDto>.Success(InventoryMapper.ToDto(supplier));
    }
}

// ── Location Command Handlers ─────────────────────────────────────────────

public sealed class CreateLocationCommandHandler(
    ILocationRepository repo,
    ILogger<CreateLocationCommandHandler> logger)
    : IRequestHandler<CreateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(CreateLocationCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetByCodeAsync(cmd.Code, ct);
        if (existing is not null)
            return Result<LocationDto>.Conflict($"A location with code '{cmd.Code}' already exists.");

        var location = new Location(cmd.Name, cmd.Code, cmd.Type, cmd.Capacity, cmd.Description, cmd.ParentLocationId);
        location.Update(cmd.Name, cmd.Description, cmd.Type, cmd.Capacity,
            cmd.ParentLocationId, cmd.Address, cmd.City, cmd.State, cmd.Country, cmd.PostalCode);

        await repo.AddAsync(location, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Location created: {LocationId} Code={Code}", location.Id, location.Code);
        return Result<LocationDto>.Success(InventoryMapper.ToDto(location));
    }
}

public sealed class UpdateLocationCommandHandler(
    ILocationRepository repo,
    ILogger<UpdateLocationCommandHandler> logger)
    : IRequestHandler<UpdateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(UpdateLocationCommand cmd, CancellationToken ct)
    {
        var location = await repo.GetByIdAsync(cmd.Id, ct);
        if (location is null) return Result<LocationDto>.NotFound($"Location {cmd.Id} not found.");

        location.Update(cmd.Name, cmd.Description, cmd.Type, cmd.Capacity,
            cmd.ParentLocationId, cmd.Address, cmd.City, cmd.State, cmd.Country, cmd.PostalCode);
        repo.Update(location);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Location updated: {LocationId}", cmd.Id);
        return Result<LocationDto>.Success(InventoryMapper.ToDto(location));
    }
}

public sealed class DeleteLocationCommandHandler(
    ILocationRepository repo,
    ILogger<DeleteLocationCommandHandler> logger)
    : IRequestHandler<DeleteLocationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLocationCommand cmd, CancellationToken ct)
    {
        var location = await repo.GetByIdAsync(cmd.Id, ct);
        if (location is null) return Result<bool>.NotFound($"Location {cmd.Id} not found.");

        repo.Remove(location);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Location deleted: {LocationId}", cmd.Id);
        return Result<bool>.Success(true);
    }
}

public sealed class ActivateLocationCommandHandler(
    ILocationRepository repo)
    : IRequestHandler<ActivateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(ActivateLocationCommand cmd, CancellationToken ct)
    {
        var location = await repo.GetByIdAsync(cmd.Id, ct);
        if (location is null) return Result<LocationDto>.NotFound($"Location {cmd.Id} not found.");
        location.Activate();
        repo.Update(location);
        await repo.SaveChangesAsync(ct);
        return Result<LocationDto>.Success(InventoryMapper.ToDto(location));
    }
}

public sealed class DeactivateLocationCommandHandler(
    ILocationRepository repo)
    : IRequestHandler<DeactivateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(DeactivateLocationCommand cmd, CancellationToken ct)
    {
        var location = await repo.GetByIdAsync(cmd.Id, ct);
        if (location is null) return Result<LocationDto>.NotFound($"Location {cmd.Id} not found.");
        location.Deactivate();
        repo.Update(location);
        await repo.SaveChangesAsync(ct);
        return Result<LocationDto>.Success(InventoryMapper.ToDto(location));
    }
}

public sealed class UpdateLocationCapacityCommandHandler(
    ILocationRepository repo)
    : IRequestHandler<UpdateLocationCapacityCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(UpdateLocationCapacityCommand cmd, CancellationToken ct)
    {
        var location = await repo.GetByIdAsync(cmd.Id, ct);
        if (location is null) return Result<LocationDto>.NotFound($"Location {cmd.Id} not found.");
        location.UpdateCapacity(cmd.Capacity);
        repo.Update(location);
        await repo.SaveChangesAsync(ct);
        return Result<LocationDto>.Success(InventoryMapper.ToDto(location));
    }
}
