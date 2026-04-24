using IMS.Modular.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Notifications.Infrastructure;

/// <summary>
/// US-066: DbContext isolado para o módulo de Notificações.
/// </summary>
public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ReadAt });
        });
    }
}
