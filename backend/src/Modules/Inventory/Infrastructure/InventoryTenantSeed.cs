using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Modules.Inventory.Infrastructure;

/// <summary>
/// US-081: Seeds demo products, suppliers, and locations for two tenants
/// so integration tests and local development can verify tenant isolation.
/// Only runs when the database is empty (idempotent guard on SKU prefix).
/// </summary>
public static class InventoryTenantSeed
{
    private const string Tenant1 = "tenant-demo-1";
    private const string Tenant2 = "tenant-demo-2";

    public static async Task SeedAsync(InventoryDbContext db, CancellationToken ct = default)
    {
        // Guard: skip if already seeded (SKUs are unique per tenant in practice,
        // but we detect by checking for our sentinel SKU).
        if (db.Products.Any(p => p.SKU == "DEMO1-WIDGET-001" || p.SKU == "DEMO2-GADGET-001"))
            return;

        // ── Tenant-demo-1 ─────────────────────────────────────────────────────

        var supplier1 = new Supplier(
            name: "Acme Corp",
            code: "ACME-001",
            contactPerson: "Alice Johnson",
            email: "alice@acme.example",
            phone: "+1-555-0101");
        supplier1.Update(
            name: "Acme Corp",
            contactPerson: "Alice Johnson",
            email: "alice@acme.example",
            phone: "+1-555-0101",
            address: "100 Main St",
            city: "Springfield",
            state: "IL",
            country: "US",
            postalCode: "62701",
            taxId: "US-TAX-001",
            creditLimit: 50_000m,
            paymentTermsDays: 30,
            notes: "Primary supplier for Tenant 1");
        supplier1.TenantId = Tenant1;

        var location1 = new Location(
            name: "Warehouse Alpha",
            code: "WH-ALPHA",
            type: LocationType.Warehouse,
            capacity: 1000,
            description: "Main warehouse for tenant-demo-1");
        location1.Update(
            name: "Warehouse Alpha",
            description: "Main warehouse for tenant-demo-1",
            type: LocationType.Warehouse,
            capacity: 1000,
            parentLocationId: null,
            address: null,
            city: "Springfield",
            state: "IL",
            country: "US",
            postalCode: null);
        location1.TenantId = Tenant1;

        var product1a = new Product(
            name: "Widget Pro",
            sku: "DEMO1-WIDGET-001",
            category: ProductCategory.Electronics,
            minimumStockLevel: 10,
            maximumStockLevel: 200,
            unitPrice: 49.99m,
            costPrice: 22.50m,
            description: "High-quality widget for tenant-demo-1",
            unit: "un",
            currency: "USD");
        product1a.TenantId = Tenant1;

        var product1b = new Product(
            name: "Gizmo Basic",
            sku: "DEMO1-GIZMO-002",
            category: ProductCategory.Tools,
            minimumStockLevel: 5,
            maximumStockLevel: 100,
            unitPrice: 19.99m,
            costPrice: 8.00m,
            description: "Basic gizmo for tenant-demo-1",
            unit: "un",
            currency: "USD");
        product1b.TenantId = Tenant1;

        // ── Tenant-demo-2 ─────────────────────────────────────────────────────

        var supplier2 = new Supplier(
            name: "Beta Supplies Ltd",
            code: "BETA-001",
            contactPerson: "Bob Williams",
            email: "bob@beta.example",
            phone: "+44-20-5550-0202");
        supplier2.Update(
            name: "Beta Supplies Ltd",
            contactPerson: "Bob Williams",
            email: "bob@beta.example",
            phone: "+44-20-5550-0202",
            address: "22 High Street",
            city: "London",
            state: "England",
            country: "GB",
            postalCode: "EC1A 1BB",
            taxId: "GB-VAT-001",
            creditLimit: 80_000m,
            paymentTermsDays: 45,
            notes: "Primary supplier for Tenant 2");
        supplier2.TenantId = Tenant2;

        var location2 = new Location(
            name: "Warehouse Beta",
            code: "WH-BETA",
            type: LocationType.Warehouse,
            capacity: 2000,
            description: "Main warehouse for tenant-demo-2");
        location2.Update(
            name: "Warehouse Beta",
            description: "Main warehouse for tenant-demo-2",
            type: LocationType.Warehouse,
            capacity: 2000,
            parentLocationId: null,
            address: null,
            city: "London",
            state: "England",
            country: "GB",
            postalCode: null);
        location2.TenantId = Tenant2;

        var product2a = new Product(
            name: "Gadget Elite",
            sku: "DEMO2-GADGET-001",
            category: ProductCategory.Electronics,
            minimumStockLevel: 20,
            maximumStockLevel: 500,
            unitPrice: 99.99m,
            costPrice: 45.00m,
            description: "Premium gadget for tenant-demo-2",
            unit: "un",
            currency: "GBP");
        product2a.TenantId = Tenant2;

        var product2b = new Product(
            name: "Tool Master 3000",
            sku: "DEMO2-TOOL-002",
            category: ProductCategory.Tools,
            minimumStockLevel: 15,
            maximumStockLevel: 300,
            unitPrice: 149.99m,
            costPrice: 70.00m,
            description: "Professional tool for tenant-demo-2",
            unit: "un",
            currency: "GBP");
        product2b.TenantId = Tenant2;

        // ── Persist ────────────────────────────────────────────────────────────

        db.Set<Supplier>().AddRange(supplier1, supplier2);
        db.Set<Location>().AddRange(location1, location2);
        db.Products.AddRange(product1a, product1b, product2a, product2b);

        await db.SaveChangesAsync(ct);
    }
}
