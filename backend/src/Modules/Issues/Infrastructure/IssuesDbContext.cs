using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Shared.Domain;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Modules.Issues.Infrastructure;

/// <summary>
/// US-081: Migrated to TenantAwareDbContext — applies global TenantId query filter on Issues.
/// </summary>
public class IssuesDbContext(
    DbContextOptions<IssuesDbContext> options,
    IMediator mediator,
    ITenantService tenantService,
    IFeatureManager featureManager)
    : TenantAwareDbContext(options, mediator, tenantService, featureManager)
{
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // US-081: Apply global tenant filter
        ApplyTenantFilter<Issue>(modelBuilder);

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.ToTable("Issues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TenantId).HasMaxLength(50);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.ReporterId);
            entity.HasIndex(e => e.TenantId);

            entity.OwnsMany(e => e.Comments, comment =>
            {
                comment.ToTable("IssueComments");
                comment.WithOwner().HasForeignKey(c => c.IssueId);
                comment.HasKey(c => c.Id);
                comment.Property(c => c.Id).ValueGeneratedOnAdd();
                comment.Property(c => c.Content).IsRequired().HasMaxLength(2000);
            });

            entity.OwnsMany(e => e.Activities, activity =>
            {
                activity.ToTable("IssueActivities");
                activity.WithOwner().HasForeignKey(a => a.IssueId);
                activity.HasKey(a => a.Id);
                activity.Property(a => a.Id).ValueGeneratedOnAdd();
                activity.Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(30);
                activity.Property(a => a.Description).IsRequired().HasMaxLength(500);
            });

            entity.OwnsMany(e => e.Tags, tag =>
            {
                tag.ToTable("IssueTags");
                tag.WithOwner();
                tag.HasKey(t => t.Id);
                tag.Property(t => t.Id).ValueGeneratedOnAdd();
                tag.Property(t => t.Name).IsRequired().HasMaxLength(50);
                tag.Property(t => t.Color).IsRequired().HasMaxLength(7);
            });

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
