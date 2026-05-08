using IMS.Modular.Modules.InventoryIssues.Domain.Entities;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.InventoryIssues.Infrastructure;

/// <summary>
/// US-080: TenantAwareDbContext — applies global TenantId query filter on InventoryIssues.
/// </summary>
public class InventoryIssuesDbContext(
    DbContextOptions<InventoryIssuesDbContext> options,
    IMediator mediator,
    ITenantService tenantService)
    : TenantAwareDbContext(options, mediator, tenantService)
{
    public DbSet<InventoryIssue> InventoryIssues => Set<InventoryIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // US-080: Apply global tenant filter
        ApplyTenantFilter<InventoryIssue>(modelBuilder);

        modelBuilder.Entity<InventoryIssue>(entity =>
        {
            entity.ToTable("InventoryIssues");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
            entity.Property(e => e.EstimatedLoss).HasPrecision(18, 2);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => e.ReporterId);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.TenantId);

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
