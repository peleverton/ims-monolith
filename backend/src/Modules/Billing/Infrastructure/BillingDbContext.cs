using IMS.Modular.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Billing.Infrastructure;

/// <summary>
/// US-091: System-level DbContext for Billing module.
/// Not tenant-aware — manages Plans, Subscriptions (per-tenant), and UsageRecords.
/// </summary>
public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Plan ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("BillingPlans");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.PriceMonthly).HasColumnType("decimal(18,2)");
            entity.Property(p => p.IsActive).HasDefaultValue(true);

            entity.HasData(
                new Plan
                {
                    Id = "free",
                    Name = "Free",
                    Description = "Up to 50 issues/month, 1 tenant",
                    MaxIssuesPerMonth = 50,
                    MaxUsers = null,
                    MaxTenants = 1,
                    PriceMonthly = 0m,
                    IsActive = true
                },
                new Plan
                {
                    Id = "pro",
                    Name = "Pro",
                    Description = "Unlimited issues, up to 5 users",
                    MaxIssuesPerMonth = null,
                    MaxUsers = 5,
                    MaxTenants = null,
                    PriceMonthly = 49m,
                    IsActive = true
                },
                new Plan
                {
                    Id = "enterprise",
                    Name = "Enterprise",
                    Description = "Unlimited everything — custom pricing",
                    MaxIssuesPerMonth = null,
                    MaxUsers = null,
                    MaxTenants = null,
                    PriceMonthly = 0m,
                    IsActive = true
                });
        });

        // ── Subscription ─────────────────────────────────────────────────────
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("BillingSubscriptions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.PlanId).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(50);
            entity.Property(s => s.StripeSubscriptionId).HasMaxLength(200);
            entity.Property(s => s.StripeCustomerId).HasMaxLength(200);

            entity.HasOne(s => s.Plan)
                  .WithMany()
                  .HasForeignKey(s => s.PlanId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.TenantId);
            entity.HasIndex(s => new { s.TenantId, s.Status });
        });

        // ── UsageRecord ───────────────────────────────────────────────────────
        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.ToTable("BillingUsageRecords");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(u => u.ResourceType).IsRequired().HasMaxLength(50);

            entity.HasIndex(u => u.TenantId);
            entity.HasIndex(u => new { u.TenantId, u.ResourceType, u.Year, u.Month }).IsUnique();
        });
    }
}
