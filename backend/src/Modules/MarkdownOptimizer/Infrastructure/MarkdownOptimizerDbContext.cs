using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.MarkdownOptimizer.Infrastructure;

public class MarkdownOptimizerDbContext(
    DbContextOptions<MarkdownOptimizerDbContext> options,
    IMediator mediator,
    ITenantService tenantService)
    : TenantAwareDbContext(options, mediator, tenantService)
{
    public DbSet<MarkdownRule> MarkdownRules => Set<MarkdownRule>();
    public DbSet<MarkdownApplication> MarkdownApplications => Set<MarkdownApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyTenantFilter<MarkdownRule>(modelBuilder);
        ApplyTenantFilter<MarkdownApplication>(modelBuilder);

        modelBuilder.Entity<MarkdownRule>(entity =>
        {
            entity.ToTable("MarkdownRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<MarkdownApplication>(entity =>
        {
            entity.ToTable("MarkdownApplications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountedPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.ProductId, e.RuleId }).HasDatabaseName("IX_MarkdownApp_Product_Rule");
            entity.HasIndex(e => e.AppliedAt);
            entity.HasIndex(e => e.TenantId);
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
