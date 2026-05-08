using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-080: Dedicated DbContext for the Tenants catalog.
/// Intentionally NOT tenant-aware (system-level, reads all tenants).
/// </summary>
public class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantEntity>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasMaxLength(64);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.Plan).HasMaxLength(30);
            e.Property(t => t.ContactEmail).HasMaxLength(200);
            e.Property(t => t.Notes).HasMaxLength(1000);
            e.HasIndex(t => t.IsActive);

            // Seed: two demo tenants used in dev/staging
            e.HasData(
                new TenantEntity
                {
                    Id = "default",
                    Name = "Default (single-tenant)",
                    Plan = "pro",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new TenantEntity
                {
                    Id = "tenant-demo-1",
                    Name = "Demo Corp Alpha",
                    Plan = "starter",
                    IsActive = true,
                    ContactEmail = "alpha@demo.local",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new TenantEntity
                {
                    Id = "tenant-demo-2",
                    Name = "Demo Corp Beta",
                    Plan = "free",
                    IsActive = true,
                    ContactEmail = "beta@demo.local",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
