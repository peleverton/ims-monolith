using Microsoft.EntityFrameworkCore;

namespace IMS.Issues.Service.Infrastructure;

/// <summary>
/// US-079: Minimal DbContext for the extracted Issues microservice.
/// Uses the same PostgreSQL database as the monolith (shared DB, separate service).
/// </summary>
public class IssuesServiceDbContext(DbContextOptions<IssuesServiceDbContext> options) : DbContext(options)
{
    public DbSet<IssueRecord> Issues => Set<IssueRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IssueRecord>(e =>
        {
            e.ToTable("Issues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Priority).HasMaxLength(20);
        });
    }
}

/// <summary>Read model for Issues (shared DB, same table as monolith).</summary>
public class IssueRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public Guid? AssigneeId { get; set; }
    public Guid ReporterId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
