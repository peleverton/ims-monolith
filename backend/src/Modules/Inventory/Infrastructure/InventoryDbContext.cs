using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Shared.Domain;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Modules.Inventory.Infrastructure;

/// <summary>
/// US-081: Migrated to TenantAwareDbContext — applies global TenantId query filter on all entities.
/// </summary>
public class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    IMediator mediator,
    ITenantService tenantService,
    IFeatureManager featureManager)
    : TenantAwareDbContext(options, mediator, tenantService, featureManager)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // US-081: Apply global tenant filter
        ApplyTenantFilter<Product>(modelBuilder);
        ApplyTenantFilter<Supplier>(modelBuilder);
        ApplyTenantFilter<Location>(modelBuilder);
        ApplyTenantFilter<StockMovement>(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.StockStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Unit).HasMaxLength(10);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.CostPrice).HasPrecision(18, 2);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.StockStatus);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContactPerson).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.ParentLocationId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovementType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.MovementType);
            entity.HasIndex(e => e.MovementDate);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
