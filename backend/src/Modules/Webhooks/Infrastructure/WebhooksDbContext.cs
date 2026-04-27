using IMS.Modular.Modules.Webhooks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IMS.Modular.Modules.Webhooks.Infrastructure;

/// <summary>
/// US-069: DbContext isolado para o módulo de Webhooks.
/// </summary>
public class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : DbContext(options)
{
    public DbSet<WebhookRegistration> WebhookRegistrations => Set<WebhookRegistration>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WebhookRegistration>(e =>
        {
            e.ToTable("WebhookRegistrations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Secret).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.OwnerId);
            e.HasIndex(x => x.IsActive);

            // Store Events list as JSON column
            e.Property(x => x.Events)
             .HasConversion(
                 v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                 v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
             .HasColumnType("text");
        });

        modelBuilder.Entity<WebhookDelivery>(e =>
        {
            e.ToTable("WebhookDeliveries");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Payload).IsRequired().HasColumnType("text");
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.WebhookRegistrationId);
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
