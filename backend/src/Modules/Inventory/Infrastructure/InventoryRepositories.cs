using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Inventory.Infrastructure;

/// <summary>
/// EF Core write repository for Product aggregate.
/// </summary>
public class ProductRepository(InventoryDbContext db) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
        => await db.Products.FirstOrDefaultAsync(p => p.SKU == sku, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await db.Products.AddAsync(product, ct);

    public void Update(Product product)
        => db.Products.Update(product);

    public void Remove(Product product)
        => db.Products.Remove(product);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

/// <summary>
/// EF Core write repository for Supplier.
/// </summary>
public class SupplierRepository(InventoryDbContext db) : ISupplierRepository
{
    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Supplier?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.Suppliers.FirstOrDefaultAsync(s => s.Code == code, ct);

    public async Task AddAsync(Supplier supplier, CancellationToken ct = default)
        => await db.Suppliers.AddAsync(supplier, ct);

    public void Update(Supplier supplier)
        => db.Suppliers.Update(supplier);

    public void Remove(Supplier supplier)
        => db.Suppliers.Remove(supplier);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

/// <summary>
/// EF Core write repository for Location.
/// </summary>
public class LocationRepository(InventoryDbContext db) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Location?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.Locations.FirstOrDefaultAsync(l => l.Code == code, ct);

    public async Task AddAsync(Location location, CancellationToken ct = default)
        => await db.Locations.AddAsync(location, ct);

    public void Update(Location location)
        => db.Locations.Update(location);

    public void Remove(Location location)
        => db.Locations.Remove(location);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

/// <summary>
/// EF Core write repository for StockMovement.
/// </summary>
public class StockMovementRepository(InventoryDbContext db) : IStockMovementRepository
{
    public async Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.StockMovements.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(StockMovement movement, CancellationToken ct = default)
        => await db.StockMovements.AddAsync(movement, ct);

    public async Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken ct = default)
        => await db.StockMovements.AddRangeAsync(movements, ct);

    public void Remove(StockMovement movement)
        => db.StockMovements.Remove(movement);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
