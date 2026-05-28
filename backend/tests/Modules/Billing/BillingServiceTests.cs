using FluentAssertions;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Modules.Billing.Domain.Entities;
using IMS.Modular.Modules.Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Tests.Modules.Billing;

/// <summary>
/// US-091: Unit tests for BillingService using EF InMemory.
/// </summary>
public class BillingServiceTests : IDisposable
{
    private readonly BillingDbContext _db;
    private readonly BillingService _sut;

    public BillingServiceTests()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BillingDbContext(options);

        // Seed plans manually (HasData doesn't run in InMemory)
        _db.Plans.AddRange(
            new Plan { Id = "free", Name = "Free", Description = "Free plan", MaxIssuesPerMonth = 50, MaxUsers = null, MaxTenants = 1, PriceMonthly = 0m, IsActive = true },
            new Plan { Id = "pro", Name = "Pro", Description = "Pro plan", MaxIssuesPerMonth = null, MaxUsers = 5, MaxTenants = null, PriceMonthly = 49m, IsActive = true },
            new Plan { Id = "enterprise", Name = "Enterprise", Description = "Enterprise plan", MaxIssuesPerMonth = null, MaxUsers = null, MaxTenants = null, PriceMonthly = 0m, IsActive = true }
        );
        _db.SaveChanges();

        _sut = new BillingService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CheckQuotaAsync_FreePlan_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var tenantId = "tenant-free-under";
        _db.Subscriptions.Add(new Subscription { TenantId = tenantId, PlanId = "free", Status = "active" });
        var now = DateTime.UtcNow;
        _db.UsageRecords.Add(new UsageRecord
        {
            TenantId = tenantId, ResourceType = "issues",
            Count = 40, Year = now.Year, Month = now.Month
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckQuotaAsync(tenantId, "issues");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckQuotaAsync_FreePlan_AtLimit_ReturnsFalse()
    {
        // Arrange
        var tenantId = "tenant-free-at-limit";
        _db.Subscriptions.Add(new Subscription { TenantId = tenantId, PlanId = "free", Status = "active" });
        var now = DateTime.UtcNow;
        _db.UsageRecords.Add(new UsageRecord
        {
            TenantId = tenantId, ResourceType = "issues",
            Count = 50, Year = now.Year, Month = now.Month
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckQuotaAsync(tenantId, "issues");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementUsageAsync_IncrementsCount()
    {
        // Arrange
        var tenantId = "tenant-increment";
        var now = DateTime.UtcNow;
        _db.UsageRecords.Add(new UsageRecord
        {
            TenantId = tenantId, ResourceType = "issues",
            Count = 5, Year = now.Year, Month = now.Month
        });
        await _db.SaveChangesAsync();

        // Act
        await _sut.IncrementUsageAsync(tenantId, "issues");

        // Assert
        var usage = await _db.UsageRecords.FirstAsync(u =>
            u.TenantId == tenantId && u.ResourceType == "issues");
        usage.Count.Should().Be(6);
    }

    [Fact]
    public async Task OverridePlanAsync_ChangesPlan()
    {
        // Arrange
        var tenantId = "tenant-override";
        _db.Subscriptions.Add(new Subscription { TenantId = tenantId, PlanId = "free", Status = "active" });
        await _db.SaveChangesAsync();

        // Act
        await _sut.OverridePlanAsync(tenantId, "pro");

        // Assert
        var subscription = await _db.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
        subscription.PlanId.Should().Be("pro");
    }
}
