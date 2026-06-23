using IMS.Modular.Modules.BinPacking.Domain.Entities;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.BinPacking.Infrastructure;

public class BinPackingDbContext(
    DbContextOptions<BinPackingDbContext> options,
    IMediator mediator,
    ITenantService tenantService)
    : TenantAwareDbContext(options, mediator, tenantService)
{
    public DbSet<PackagingType> PackagingTypes => Set<PackagingType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyTenantFilter<PackagingType>(modelBuilder);

        modelBuilder.Entity<PackagingType>(entity =>
        {
            entity.ToTable("PackagingTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MaxLengthCm).HasPrecision(10, 2);
            entity.Property(e => e.MaxWidthCm).HasPrecision(10, 2);
            entity.Property(e => e.MaxHeightCm).HasPrecision(10, 2);
            entity.Property(e => e.MaxWeightKg).HasPrecision(10, 3);
            entity.Property(e => e.CostPerUnit).HasPrecision(18, 2);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.CapacityVolumeCm3);
        });
    }
}
