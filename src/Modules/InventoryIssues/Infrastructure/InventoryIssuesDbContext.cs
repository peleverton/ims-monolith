using IMS.Modular.Modules.InventoryIssues.Domain.Entities;
using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.InventoryIssues.Infrastructure;

// US-022: herda BaseDbContext — SaveChangesAsync com domain event dispatch centralizado
public class InventoryIssuesDbContext(DbContextOptions<InventoryIssuesDbContext> options, IMediator mediator)
    : BaseDbContext(options, mediator)
{
    public DbSet<InventoryIssue> InventoryIssues => Set<InventoryIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
