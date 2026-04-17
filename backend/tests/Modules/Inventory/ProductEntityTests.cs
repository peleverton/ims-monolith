using FluentAssertions;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Tests.Modules.Inventory;

/// <summary>
/// Unit tests for the Product aggregate root.
/// Pattern: AAA (Arrange / Act / Assert)
/// </summary>
public class ProductEntityTests
{
    private static Product MakeProduct(int minStock = 10, int maxStock = 100, string sku = "SKU-001")
        => new("Widget A", sku, ProductCategory.Electronics, minStock, maxStock, 29.99m, 15.00m);

    // ── Construction ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidParams_CreatesProductWithOutOfStockStatus()
    {
        var product = MakeProduct();

        product.Name.Should().Be("Widget A");
        product.SKU.Should().Be("SKU-001");
        product.CurrentStock.Should().Be(0);
        product.StockStatus.Should().Be(StockStatus.OutOfStock);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        var act = () => new Product("", "SKU-X", ProductCategory.Electronics, 5, 50, 10m, 5m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptySku_ThrowsArgumentException()
    {
        var act = () => new Product("Widget", "", ProductCategory.Electronics, 5, 50, 10m, 5m);
        act.Should().Throw<ArgumentException>();
    }

    // ── Stock — StockIn ────────────────────────────────────────────

    [Fact]
    public void AdjustStock_StockIn_IncreasesCurrentStock()
    {
        var product = MakeProduct(minStock: 5, maxStock: 100);
        product.AdjustStock(50, StockMovementType.StockIn);
        product.CurrentStock.Should().Be(50);
    }

    [Fact]
    public void AdjustStock_StockIn_StatusBecomesInStock()
    {
        var product = MakeProduct(minStock: 5, maxStock: 100);
        product.AdjustStock(50, StockMovementType.StockIn);
        product.StockStatus.Should().Be(StockStatus.InStock);
    }

    [Fact]
    public void AdjustStock_StockIn_ExceedsMax_StatusBecomesOverstock()
    {
        var product = MakeProduct(minStock: 5, maxStock: 30);
        product.AdjustStock(100, StockMovementType.StockIn);
        product.StockStatus.Should().Be(StockStatus.Overstock);
    }

    // ── Stock — StockOut ───────────────────────────────────────────

    [Fact]
    public void AdjustStock_StockOut_DecreasesCurrentStock()
    {
        var product = MakeProduct();
        product.AdjustStock(50, StockMovementType.StockIn);
        product.AdjustStock(20, StockMovementType.StockOut);
        product.CurrentStock.Should().Be(30);
    }

    [Fact]
    public void AdjustStock_StockOut_NeverGoesNegative()
    {
        var product = MakeProduct();
        product.AdjustStock(5, StockMovementType.StockIn);
        product.AdjustStock(100, StockMovementType.StockOut);
        product.CurrentStock.Should().Be(0);
    }

    [Fact]
    public void AdjustStock_DropsBelowMin_StatusBecomesLowStock()
    {
        var product = MakeProduct(minStock: 10, maxStock: 100);
        product.AdjustStock(50, StockMovementType.StockIn);
        product.AdjustStock(45, StockMovementType.StockOut); // 5 remaining < min 10
        product.StockStatus.Should().Be(StockStatus.LowStock);
    }

    [Fact]
    public void AdjustStock_DropsToZero_StatusBecomesOutOfStock()
    {
        var product = MakeProduct();
        product.AdjustStock(10, StockMovementType.StockIn);
        product.AdjustStock(10, StockMovementType.StockOut);
        product.StockStatus.Should().Be(StockStatus.OutOfStock);
    }

    // ── Stock — StockMovementType outgoing variants ────────────────

    [Theory]
    [InlineData(StockMovementType.Sale)]
    [InlineData(StockMovementType.Damage)]
    [InlineData(StockMovementType.Loss)]
    [InlineData(StockMovementType.Expired)]
    [InlineData(StockMovementType.Return)]
    public void AdjustStock_OutgoingTypes_DecreasesStock(StockMovementType type)
    {
        var product = MakeProduct();
        product.AdjustStock(50, StockMovementType.StockIn);
        product.AdjustStock(10, type);
        product.CurrentStock.Should().Be(40);
    }

    // ── Domain Events ──────────────────────────────────────────────

    [Fact]
    public void Constructor_RaisesProductCreatedEvent()
    {
        var product = MakeProduct();
        product.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "ProductCreatedEvent");
    }

    [Fact]
    public void AdjustStock_StockIn_RaisesStockChangedEvent()
    {
        var product = MakeProduct();
        product.ClearDomainEvents();
        product.AdjustStock(20, StockMovementType.StockIn);
        product.DomainEvents.Should().Contain(e => e.GetType().Name == "StockChangedEvent");
    }

    [Fact]
    public void AdjustStock_DropsToZero_RaisesOutOfStockEvent()
    {
        var product = MakeProduct();
        product.AdjustStock(10, StockMovementType.StockIn);
        product.ClearDomainEvents();
        product.AdjustStock(10, StockMovementType.StockOut);
        product.DomainEvents.Should().Contain(e => e.GetType().Name == "OutOfStockEvent");
    }

    [Fact]
    public void AdjustStock_DropsBelowMin_RaisesLowStockAlertEvent()
    {
        var product = MakeProduct(minStock: 10, maxStock: 100);
        product.AdjustStock(50, StockMovementType.StockIn);
        product.ClearDomainEvents();
        product.AdjustStock(45, StockMovementType.StockOut);
        product.DomainEvents.Should().Contain(e => e.GetType().Name == "LowStockAlertEvent");
    }

    // ── Pricing ────────────────────────────────────────────────────

    [Fact]
    public void UpdatePricing_ValidPrices_UpdatesAndRaisesPriceChangedEvent()
    {
        var product = MakeProduct();
        product.ClearDomainEvents();
        product.UpdatePricing(49.99m, 22.00m);
        product.UnitPrice.Should().Be(49.99m);
        product.CostPrice.Should().Be(22.00m);
        product.DomainEvents.Should().Contain(e => e.GetType().Name == "PriceChangedEvent");
    }

    // ── Lifecycle ──────────────────────────────────────────────────

    [Fact]
    public void Discontinue_SetsInactiveAndStatusDiscontinued()
    {
        var product = MakeProduct();
        product.Discontinue();
        product.IsActive.Should().BeFalse();
        product.StockStatus.Should().Be(StockStatus.Discontinued);
    }

    [Fact]
    public void Discontinue_RaisesProductDiscontinuedEvent()
    {
        var product = MakeProduct();
        product.ClearDomainEvents();
        product.Discontinue();
        product.DomainEvents.Should().Contain(e => e.GetType().Name == "ProductDiscontinuedEvent");
    }

    // ── Transfer ───────────────────────────────────────────────────

    [Fact]
    public void TransferStock_UpdatesLocationId()
    {
        var product = MakeProduct();
        var newLocation = Guid.NewGuid();
        product.TransferStock(5, null, newLocation);
        product.LocationId.Should().Be(newLocation);
    }
}
