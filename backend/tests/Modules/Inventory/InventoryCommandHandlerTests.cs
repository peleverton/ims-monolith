using FluentAssertions;
using IMS.Modular.Modules.Inventory.Application.Commands;
using IMS.Modular.Modules.Inventory.Application.Handlers;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IMS.Modular.Tests.Modules.Inventory;

/// <summary>
/// Unit tests for Inventory command handlers using EF InMemory.
/// Pattern: AAA (Arrange / Act / Assert)
/// </summary>
public class InventoryCommandHandlerTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly Mock<IMediator> _mediator = new();

    public InventoryCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options, _mediator.Object);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────

    private CreateProductCommandHandler ProductHandler() =>
        new(new ProductRepository(_db), NullLogger<CreateProductCommandHandler>.Instance);

    private static CreateProductCommand ProductCommand(string sku = "SKU-TEST-001") =>
        new("Widget", sku, ProductCategory.Electronics,
            MinimumStockLevel: 5, MaximumStockLevel: 100,
            UnitPrice: 29.99m, CostPrice: 15m,
            Description: null, Barcode: null,
            Unit: "un", Currency: "BRL",
            LocationId: null, SupplierId: null, ExpiryDate: null);

    // ── CreateProductCommand ──────────────────────────────────────

    [Fact]
    public async Task CreateProduct_ValidCommand_ReturnsSuccess()
    {
        var result = await ProductHandler().Handle(ProductCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Widget");
        result.Value.SKU.Should().Be("SKU-TEST-001");
    }

    [Fact]
    public async Task CreateProduct_ValidCommand_PersistsToDatabase()
    {
        await ProductHandler().Handle(ProductCommand("SKU-PERSIST"), CancellationToken.None);

        var saved = await _db.Products.FirstOrDefaultAsync(p => p.SKU == "SKU-PERSIST");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_DuplicateSKU_ReturnsConflict()
    {
        await ProductHandler().Handle(ProductCommand("DUP-001"), CancellationToken.None);
        var result = await ProductHandler().Handle(ProductCommand("DUP-001"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(409);
    }

    // ── UpdateProductCommand ──────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_ExistingProduct_UpdatesName()
    {
        var created = (await ProductHandler().Handle(ProductCommand("SKU-UPD"), CancellationToken.None)).Value!;

        var updateHandler = new UpdateProductCommandHandler(
            new ProductRepository(_db), NullLogger<UpdateProductCommandHandler>.Instance);

        var cmd = new UpdateProductCommand(
            created.Id, "Updated Widget", null, ProductCategory.Electronics,
            5, 100, null, "un", "BRL", null, null, null);

        var result = await updateHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Widget");
    }

    [Fact]
    public async Task UpdateProduct_NonExistent_ReturnsNotFound()
    {
        var updateHandler = new UpdateProductCommandHandler(
            new ProductRepository(_db), NullLogger<UpdateProductCommandHandler>.Instance);

        var cmd = new UpdateProductCommand(
            Guid.NewGuid(), "Ghost", null, ProductCategory.Electronics,
            5, 100, null, "un", "BRL", null, null, null);

        var result = await updateHandler.Handle(cmd, CancellationToken.None);
        result.ErrorCode.Should().Be(404);
    }

    // ── DeleteProductCommand ──────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_ExistingProduct_RemovesFromDb()
    {
        var created = (await ProductHandler().Handle(ProductCommand("SKU-DEL"), CancellationToken.None)).Value!;

        var deleteHandler = new DeleteProductCommandHandler(
            new ProductRepository(_db), NullLogger<DeleteProductCommandHandler>.Instance);

        var result = await deleteHandler.Handle(new DeleteProductCommand(created.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Products.FindAsync(created.Id)).Should().BeNull();
    }

    // ── AdjustStockCommand ────────────────────────────────────────

    [Fact]
    public async Task AdjustStock_StockIn_IncreasesCurrentStock()
    {
        var created = (await ProductHandler().Handle(ProductCommand("SKU-STK"), CancellationToken.None)).Value!;

        var adjustHandler = new AdjustStockCommandHandler(
            new ProductRepository(_db),
            new StockMovementRepository(_db),
            NullLogger<AdjustStockCommandHandler>.Instance);

        var result = await adjustHandler.Handle(
            new AdjustStockCommand(created.Id, 50, StockMovementType.StockIn, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStock.Should().Be(50);
    }

    [Fact]
    public async Task AdjustStock_StockOut_DecreasesCurrentStock()
    {
        var created = (await ProductHandler().Handle(ProductCommand("SKU-STK2"), CancellationToken.None)).Value!;

        var adjustHandler = new AdjustStockCommandHandler(
            new ProductRepository(_db),
            new StockMovementRepository(_db),
            NullLogger<AdjustStockCommandHandler>.Instance);

        await adjustHandler.Handle(
            new AdjustStockCommand(created.Id, 50, StockMovementType.StockIn, null, null),
            CancellationToken.None);

        var result = await adjustHandler.Handle(
            new AdjustStockCommand(created.Id, 20, StockMovementType.StockOut, null, null),
            CancellationToken.None);

        result.Value!.CurrentStock.Should().Be(30);
    }

    // ── CreateSupplierCommand ─────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_ValidCommand_ReturnsSuccess()
    {
        var handler = new CreateSupplierCommandHandler(
            new SupplierRepository(_db), NullLogger<CreateSupplierCommandHandler>.Instance);

        var cmd = new CreateSupplierCommand(
            "ACME Corp", "SUP-001", "John Doe", "john@acme.com", "+55 11 99999-0000",
            null, null, null, "Brazil", null, null, 50000m, 30, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("ACME Corp");
        result.Value.Code.Should().Be("SUP-001");
    }

    [Fact]
    public async Task CreateSupplier_DuplicateCode_ReturnsConflict()
    {
        var handler = new CreateSupplierCommandHandler(
            new SupplierRepository(_db), NullLogger<CreateSupplierCommandHandler>.Instance);

        var cmd = new CreateSupplierCommand(
            "Supplier X", "SUP-DUP", null, null, null,
            null, null, null, null, null, null, 0m, 30, null);

        await handler.Handle(cmd, CancellationToken.None);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ErrorCode.Should().Be(409);
    }

    // ── CreateLocationCommand ─────────────────────────────────────

    [Fact]
    public async Task CreateLocation_ValidCommand_PersistsLocation()
    {
        var handler = new CreateLocationCommandHandler(
            new LocationRepository(_db), NullLogger<CreateLocationCommandHandler>.Instance);

        var cmd = new CreateLocationCommand(
            "Warehouse A", "LOC-001", LocationType.Warehouse, 1000,
            null, null, null, null, null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Locations.CountAsync()).Should().Be(1);
    }
}
