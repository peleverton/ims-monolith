using IMS.Modular.Shared.MultiTenancy;
using IMS.Modular.Shared.Domain;
using NSubstitute;
using Xunit;

namespace IMS.Modular.Tests.Shared.MultiTenancy;

/// <summary>US-081: Tests for multi-tenancy Phase 2 — TenantId on all entities.</summary>
public class TenantPhase2Tests
{
    [Fact]
    public void BaseEntity_HasTenantId_Property()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Null(entity.TenantId);   // default is null (shared/global)
    }

    [Fact]
    public void BaseEntity_ImplementsITenantEntity()
    {
        // Arrange
        var entity = new TestEntity();

        // Assert
        Assert.IsAssignableFrom<ITenantEntity>(entity);
    }

    [Fact]
    public void TenantId_CanBeSetOnEntity()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.TenantId = "tenant-alpha";

        // Assert
        Assert.Equal("tenant-alpha", entity.TenantId);
    }

    [Fact]
    public void ITenantService_ReturnsCurrentTenant()
    {
        // Arrange
        var tenantService = Substitute.For<ITenantService>();
        tenantService.TenantId.Returns("tenant-beta");

        // Act & Assert
        Assert.Equal("tenant-beta", tenantService.TenantId);
    }

    [Fact]
    public void TenantContext_SetAndGet_TenantId()
    {
        // Arrange
        var context = new TenantContext();

        // Act
        context.SetTenant("tenant-gamma");

        // Assert
        Assert.Equal("tenant-gamma", context.TenantId);
    }

    [Fact]
    public void TenantContext_DefaultTenantId_IsNull()
    {
        // Arrange & Act
        var context = new TenantContext();

        // Assert
        Assert.Null(context.TenantId);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private class TestEntity : BaseEntity
    {
        public string? Name { get; set; }
    }
}
