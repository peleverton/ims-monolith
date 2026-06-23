using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.AnomalyDetection.Infrastructure;

public class AnomalyDetectionDbContext(
    DbContextOptions<AnomalyDetectionDbContext> options,
    IMediator mediator,
    ITenantService tenantService)
    : TenantAwareDbContext(options, mediator, tenantService)
{
    public DbSet<AnomalyAlert> AnomalyAlerts => Set<AnomalyAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyTenantFilter<AnomalyAlert>(modelBuilder);

        modelBuilder.Entity<AnomalyAlert>(entity =>
        {
            entity.ToTable("AnomalyAlerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.WindowDescription).HasMaxLength(100);
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(200);
            entity.Property(e => e.Resolution).HasMaxLength(2000);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
