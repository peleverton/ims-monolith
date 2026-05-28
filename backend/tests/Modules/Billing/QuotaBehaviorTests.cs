using FluentAssertions;
using IMS.Modular.Modules.Billing.Application.Behaviors;
using IMS.Modular.Modules.Billing.Application.Interfaces;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Moq;

namespace IMS.Modular.Tests.Modules.Billing;

/// <summary>
/// US-091: Unit tests for QuotaBehavior MediatR pipeline.
/// </summary>
public class QuotaBehaviorTests
{
    private readonly Mock<ITenantService> _tenantServiceMock = new();
    private readonly Mock<IBillingService> _billingServiceMock = new();

    [Fact]
    public async Task NonIRequiresQuota_PassesThrough()
    {
        // Arrange
        var behavior = new QuotaBehavior<NonQuotaRequest, Unit>(
            _tenantServiceMock.Object, _billingServiceMock.Object);
        var wasCalled = false;
        RequestHandlerDelegate<Unit> next = (ct) => { wasCalled = true; return Task.FromResult(Unit.Value); };

        // Act
        await behavior.Handle(new NonQuotaRequest(), next, CancellationToken.None);

        // Assert
        wasCalled.Should().BeTrue();
        _billingServiceMock.Verify(b => b.CheckQuotaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IRequiresQuota_UnderQuota_PassesThrough()
    {
        // Arrange
        _tenantServiceMock.Setup(t => t.TenantId).Returns("tenant-1");
        _billingServiceMock.Setup(b => b.CheckQuotaAsync("tenant-1", "issues")).ReturnsAsync(true);

        var behavior = new QuotaBehavior<QuotaRequest, Unit>(
            _tenantServiceMock.Object, _billingServiceMock.Object);
        var wasCalled = false;
        RequestHandlerDelegate<Unit> next = (ct) => { wasCalled = true; return Task.FromResult(Unit.Value); };

        // Act
        await behavior.Handle(new QuotaRequest(), next, CancellationToken.None);

        // Assert
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task IRequiresQuota_OverQuota_ThrowsQuotaExceededException()
    {
        // Arrange
        _tenantServiceMock.Setup(t => t.TenantId).Returns("tenant-over");
        _billingServiceMock.Setup(b => b.CheckQuotaAsync("tenant-over", "issues")).ReturnsAsync(false);

        var behavior = new QuotaBehavior<QuotaRequest, Unit>(
            _tenantServiceMock.Object, _billingServiceMock.Object);
        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        var act = async () => await behavior.Handle(new QuotaRequest(), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<QuotaExceededException>();
    }
}

// ── Test request types ────────────────────────────────────────────────────────

public record NonQuotaRequest : IRequest<Unit>;

public record QuotaRequest : IRequest<Unit>, IRequiresQuota;
