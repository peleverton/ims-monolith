using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Inventory.Application;

/// <summary>
/// US-078: Seed de dados por tenant para demonstrar isolamento multi-tenant.
/// Cria produtos de exemplo para "tenant-alpha" e "tenant-beta" se ainda não existirem.
/// </summary>
public static class TenantInventorySeed
{
    private static readonly Guid TenantAlphaLocationId = Guid.Parse("A0000001-0000-0000-0000-000000000001");
    private static readonly Guid TenantBetaLocationId  = Guid.Parse("B0000001-0000-0000-0000-000000000001");
    private static readonly Guid TenantAlphaSupplierId = Guid.Parse("A0000002-0000-0000-0000-000000000001");
    private static readonly Guid TenantBetaSupplierId  = Guid.Parse("B0000002-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await SeedTenantAsync(db, logger,
            locationId: TenantAlphaLocationId,
            locationName: "Alpha Warehouse", locationCode: "ALPHA-WH",
            supplierId: TenantAlphaSupplierId,
            supplierName: "Alpha Supplier Co.", supplierCode: "ALPHA-SUP",
            supplierEmail: "supplier@alpha.com",
            products: new[]
            {
                ("Laptop Alpha Pro",     "ALPHA-LAP-001", ProductCategory.Electronics),
                ("Alpha Desk Chair",     "ALPHA-CHR-001", ProductCategory.Furniture),
                ("Alpha Safety Helmet",  "ALPHA-SAF-001", ProductCategory.Tools),
            },
            tenantLabel: "tenant-alpha");

        await SeedTenantAsync(db, logger,
            locationId: TenantBetaLocationId,
            locationName: "Beta Distribution Center", locationCode: "BETA-DC",
            supplierId: TenantBetaSupplierId,
            supplierName: "Beta Supplies Ltd.", supplierCode: "BETA-SUP",
            supplierEmail: "supplier@beta.com",
            products: new[]
            {
                ("Beta Server Rack",         "BETA-SRV-001", ProductCategory.Electronics),
                ("Beta Office Desk",         "BETA-DSK-001", ProductCategory.Furniture),
                ("Beta Fire Extinguisher",   "BETA-FEX-001", ProductCategory.Tools),
            },
            tenantLabel: "tenant-beta");
    }

    private static async Task SeedTenantAsync(
        InventoryDbContext db,
        ILogger logger,
        Guid locationId, string locationName, string locationCode,
        Guid supplierId, string supplierName, string supplierCode, string supplierEmail,
        (string Name, string SKU, ProductCategory Category)[] products,
        string tenantLabel)
    {
        if (!await db.Locations.AnyAsync(l => l.Id == locationId))
        {
            db.Locations.Add(new Location(locationName, locationCode, LocationType.Warehouse, 1000));
            logger.LogInformation("[TenantSeed] Localização criada para {Tenant}", tenantLabel);
        }

        if (!await db.Suppliers.AnyAsync(s => s.Id == supplierId))
        {
            db.Suppliers.Add(new Supplier(supplierName, supplierCode, null, supplierEmail, null));
            logger.LogInformation("[TenantSeed] Fornecedor criado para {Tenant}", tenantLabel);
        }

        await db.SaveChangesAsync();

        foreach (var (name, sku, category) in products)
        {
            if (!await db.Products.AnyAsync(p => p.SKU == sku))
            {
                var product = new Product(name, sku, category, 5, 100, 1000m, 700m);
                product.SetSupplier(supplierId);
                db.Products.Add(product);
                logger.LogInformation("[TenantSeed] Produto {SKU} criado para {Tenant}", sku, tenantLabel);
            }
        }

        await db.SaveChangesAsync();
    }
}
