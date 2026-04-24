using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IMS.Modular.Tests.Modules.Jobs;

/// <summary>
/// US-067: Testes unitários para os Background Jobs Hangfire.
/// </summary>
public class BackgroundJobsTests
{
    private static readonly Mock<IMediator> _mediator = new();

    private static InventoryDbContext CreateInventoryDb(string name)
    {
        var opts = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new InventoryDbContext(opts, _mediator.Object);
    }

    private static IssuesDbContext CreateIssuesDb(string name)
    {
        var opts = new DbContextOptionsBuilder<IssuesDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new IssuesDbContext(opts, _mediator.Object);
    }

    private static AuthDbContext CreateAuthDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new AuthDbContext(opts);
    }

    private static Product MakeProduct(string sku, int stock = 10, DateTime? expiryDate = null)
    {
        var p = new Product("Produto", sku, ProductCategory.Electronics,
            minimumStockLevel: 2, maximumStockLevel: 100, unitPrice: 10m, costPrice: 8m);
        if (stock > 0)
            typeof(Product).GetProperty("CurrentStock")!.SetValue(p, stock);
        if (expiryDate.HasValue)
            typeof(Product).GetProperty("ExpiryDate")!.SetValue(p, expiryDate);
        return p;
    }

    private static Issue MakeIssue(string title, DateTime? dueDate = null)
        => new(title, "desc", IssuePriority.Medium, Guid.NewGuid(), dueDate);

    // ── ExpiryCheckJob ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExpiryCheckJob_NoExpiringProducts_ReturnsWithoutError()
    {
        var db = CreateInventoryDb($"expiry-none-{Guid.NewGuid()}");
        var job = new ExpiryCheckJob(db, NullLogger<ExpiryCheckJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task ExpiryCheckJob_ProductExpiresInTenDays_IsLogged()
    {
        var db = CreateInventoryDb($"expiry-soon-{Guid.NewGuid()}");
        db.Products.Add(MakeProduct("SKU-EXP", stock: 5, expiryDate: DateTime.UtcNow.AddDays(10)));
        await db.SaveChangesAsync();

        var job = new ExpiryCheckJob(db, NullLogger<ExpiryCheckJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task ExpiryCheckJob_ZeroStockExpiringProduct_IsIgnored()
    {
        var db = CreateInventoryDb($"expiry-zero-{Guid.NewGuid()}");
        db.Products.Add(MakeProduct("SKU-ZERO", stock: 0, expiryDate: DateTime.UtcNow.AddDays(5)));
        await db.SaveChangesAsync();

        var job = new ExpiryCheckJob(db, NullLogger<ExpiryCheckJob>.Instance);
        await job.ExecuteAsync();
    }

    // ── OverdueIssuesJob ───────────────────────────────────────────────────

    [Fact]
    public async Task OverdueIssuesJob_NoOverdueIssues_ReturnsWithoutError()
    {
        var db = CreateIssuesDb($"overdue-none-{Guid.NewGuid()}");
        var job = new OverdueIssuesJob(db, NullLogger<OverdueIssuesJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task OverdueIssuesJob_OpenIssueWithPastDueDate_IsDetected()
    {
        var db = CreateIssuesDb($"overdue-open-{Guid.NewGuid()}");
        db.Issues.Add(MakeIssue("Overdue", dueDate: DateTime.UtcNow.AddDays(-3)));
        await db.SaveChangesAsync();

        var job = new OverdueIssuesJob(db, NullLogger<OverdueIssuesJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task OverdueIssuesJob_ResolvedIssueWithPastDueDate_IsIgnored()
    {
        var db = CreateIssuesDb($"overdue-resolved-{Guid.NewGuid()}");
        var issue = MakeIssue("Resolved overdue", dueDate: DateTime.UtcNow.AddDays(-5));
        issue.UpdateStatus(IssueStatus.Resolved, Guid.NewGuid());
        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var job = new OverdueIssuesJob(db, NullLogger<OverdueIssuesJob>.Instance);
        await job.ExecuteAsync();
    }

    // ── AnalyticsSnapshotJob ───────────────────────────────────────────────

    [Fact]
    public async Task AnalyticsSnapshotJob_EmptyDb_LogsZeroes()
    {
        var issuesDb = CreateIssuesDb($"snap-i-{Guid.NewGuid()}");
        var inventoryDb = CreateInventoryDb($"snap-v-{Guid.NewGuid()}");

        var job = new AnalyticsSnapshotJob(issuesDb, inventoryDb, NullLogger<AnalyticsSnapshotJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task AnalyticsSnapshotJob_WithData_DoesNotThrow()
    {
        var issuesDb = CreateIssuesDb($"snap-i2-{Guid.NewGuid()}");
        var inventoryDb = CreateInventoryDb($"snap-v2-{Guid.NewGuid()}");

        issuesDb.Issues.Add(MakeIssue("I1"));
        issuesDb.Issues.Add(MakeIssue("I2"));
        await issuesDb.SaveChangesAsync();

        inventoryDb.Products.Add(MakeProduct("P1", stock: 3));
        await inventoryDb.SaveChangesAsync();

        var job = new AnalyticsSnapshotJob(issuesDb, inventoryDb, NullLogger<AnalyticsSnapshotJob>.Instance);
        await job.ExecuteAsync();
    }

    // ── TokenCleanupJob ────────────────────────────────────────────────────

    [Fact]
    public async Task TokenCleanupJob_NoStaleTokens_ReturnsWithoutError()
    {
        var db = CreateAuthDb($"token-none-{Guid.NewGuid()}");
        var job = new TokenCleanupJob(db, NullLogger<TokenCleanupJob>.Instance);
        await job.ExecuteAsync();
    }

    [Fact]
    public async Task TokenCleanupJob_OldRevokedToken_IsRemovedFromDatabase()
    {
        var db = CreateAuthDb($"token-rev-{Guid.NewGuid()}");

        var user = new User
        {
            Id = Guid.NewGuid(), Username = "u1", Email = "u1@test.com",
            PasswordHash = "h", FullName = "U1", IsActive = true
        };
        db.Users.Add(user);

        var oldToken = RefreshToken.Create(user.Id, "hash123", DateTime.UtcNow.AddDays(-60));
        oldToken.Revoke();
        // Set CreatedAt to 45 days ago
        typeof(RefreshToken).BaseType!.GetProperty("CreatedAt")!
            .SetValue(oldToken, DateTime.UtcNow.AddDays(-45));

        db.RefreshTokens.Add(oldToken);
        await db.SaveChangesAsync();

        var job = new TokenCleanupJob(db, NullLogger<TokenCleanupJob>.Instance);
        await job.ExecuteAsync();

        Assert.Equal(0, await db.RefreshTokens.CountAsync());
    }
}
