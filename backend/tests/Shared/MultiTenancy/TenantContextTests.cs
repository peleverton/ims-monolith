using IMS.Modular.Shared.MultiTenancy;

namespace IMS.Modular.Tests.Shared.MultiTenancy;

public class TenantContextTests
{
    [Fact]
    public void SetTenant_WithValidId_SetsTenantId()
    {
        var ctx = new TenantContext();
        ctx.SetTenant("tenant-abc");
        Assert.Equal("tenant-abc", ctx.TenantId);
    }

    [Fact]
    public void SetTenant_WithEmptyString_ThrowsArgumentException()
    {
        var ctx = new TenantContext();
        Assert.Throws<ArgumentException>(() => ctx.SetTenant(""));
    }

    [Fact]
    public void SetTenant_WithWhitespace_ThrowsArgumentException()
    {
        var ctx = new TenantContext();
        Assert.Throws<ArgumentException>(() => ctx.SetTenant("   "));
    }

    [Fact]
    public void Clear_ResetsTenantId()
    {
        var ctx = new TenantContext();
        ctx.SetTenant("tenant-abc");
        ctx.Clear();
        Assert.Null(ctx.TenantId);
    }

    [Fact]
    public void HasTenant_ReturnsFalse_WhenNoTenantSet()
    {
        var ctx = new TenantContext();
        Assert.False(ctx.HasTenant);
    }

    [Fact]
    public void HasTenant_ReturnsTrue_WhenTenantSet()
    {
        var ctx = new TenantContext();
        ctx.SetTenant("tenant-xyz");
        Assert.True(ctx.HasTenant);
    }

    [Fact]
    public void ITenantService_ResolvedAsTenantContext()
    {
        // Verify TenantContext satisfies ITenantService interface
        var ctx = new TenantContext();
        ITenantService service = ctx;
        ctx.SetTenant("tenant-1");
        Assert.Equal("tenant-1", service.TenantId);
    }
}
