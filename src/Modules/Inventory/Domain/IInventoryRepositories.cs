using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Modules.Inventory.Domain;

/// <summary>
/// Write repository for Product aggregate — EF Core.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
    void Remove(Product product);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Write repository for Supplier — EF Core.
/// </summary>
public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Supplier?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Supplier supplier, CancellationToken ct = default);
    void Update(Supplier supplier);
    void Remove(Supplier supplier);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Write repository for Location — EF Core.
/// </summary>
public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Location?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Location location, CancellationToken ct = default);
    void Update(Location location);
    void Remove(Location location);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Write repository for StockMovement — EF Core.
/// </summary>
public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(StockMovement movement, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken ct = default);
    void Remove(StockMovement movement);
    Task SaveChangesAsync(CancellationToken ct = default);
}
