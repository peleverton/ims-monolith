using IMS.Modular.Modules.DemandForecasting.Domain.Entities;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.DemandForecasting.Infrastructure;

public class DemandForecastingDbContext(
    DbContextOptions<DemandForecastingDbContext> options,
    IMediator mediator,
    ITenantService tenantService)
    : TenantAwareDbContext(options, mediator, tenantService)
{
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyTenantFilter<DemandForecast>(modelBuilder);

        modelBuilder.Entity<DemandForecast>(entity =>
        {
            entity.ToTable("DemandForecasts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AvgDailyDemand7d).HasPrecision(18, 4);
            entity.Property(e => e.AvgDailyDemand14d).HasPrecision(18, 4);
            entity.Property(e => e.AvgDailyDemand30d).HasPrecision(18, 4);
            entity.Property(e => e.WeightedAvgDailyDemand).HasPrecision(18, 4);
            entity.Property(e => e.RiskLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Strategy).HasMaxLength(50);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.EstimatedStockoutDate);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
