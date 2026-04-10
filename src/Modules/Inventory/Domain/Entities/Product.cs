using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Domain.Events;

namespace IMS.Modular.Modules.Inventory.Domain.Entities;

/// <summary>
/// Product — Aggregate Root for inventory management.
/// Owns stock levels, pricing, and status calculation.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string SKU { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public string? Description { get; private set; }
    public ProductCategory Category { get; private set; }

    public int CurrentStock { get; private set; }
    public int MinimumStockLevel { get; private set; }
    public int MaximumStockLevel { get; private set; }

    public decimal UnitPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public string Unit { get; private set; } = "un";
    public string Currency { get; private set; } = "BRL";

    public Guid? LocationId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public StockStatus StockStatus { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Product() { }

    public Product(
        string name,
        string sku,
        ProductCategory category,
        int minimumStockLevel,
        int maximumStockLevel,
        decimal unitPrice,
        decimal costPrice,
        string? description = null,
        string? barcode = null,
        string unit = "un",
        string currency = "BRL")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        Name = name;
        SKU = sku;
        Category = category;
        MinimumStockLevel = minimumStockLevel;
        MaximumStockLevel = maximumStockLevel;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        Description = description;
        Barcode = barcode;
        Unit = unit;
        Currency = currency;
        StockStatus = StockStatus.OutOfStock;

        AddDomainEvent(new ProductCreatedEvent(Id, name, sku, category));
    }

    // ── Stock Management ────────────────────────────────────────────

    public void AdjustStock(int quantity, StockMovementType movementType)
    {
        var previousStock = CurrentStock;

        // Outgoing movements subtract from stock; incoming add.
        var delta = movementType switch
        {
            StockMovementType.StockOut or
            StockMovementType.Sale     or
            StockMovementType.Damage   or
            StockMovementType.Loss     or
            StockMovementType.Expired  or
            StockMovementType.Return   => -Math.Abs(quantity),
            _                          =>  Math.Abs(quantity)   // StockIn, Adjustment, Transfer, InitialStock, etc.
        };

        CurrentStock += delta;
        if (CurrentStock < 0) CurrentStock = 0;

        UpdatedAt = DateTime.UtcNow;
        RecalculateStockStatus();

        AddDomainEvent(new StockChangedEvent(Id, SKU, previousStock, CurrentStock, movementType));

        // Alert events
        if (CurrentStock == 0 && previousStock > 0)
            AddDomainEvent(new OutOfStockEvent(Id, SKU, Name));
        else if (CurrentStock <= MinimumStockLevel && CurrentStock > 0 && previousStock > MinimumStockLevel)
            AddDomainEvent(new LowStockAlertEvent(Id, SKU, Name, CurrentStock, MinimumStockLevel));
        else if (previousStock == 0 && CurrentStock > 0)
            AddDomainEvent(new StockReplenishedEvent(Id, SKU, previousStock, CurrentStock));
    }

    public void TransferStock(int quantity, Guid? fromLocationId, Guid toLocationId)
    {
        AddDomainEvent(new StockTransferInitiatedEvent(Id, SKU, fromLocationId, toLocationId, quantity));
        LocationId = toLocationId;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockTransferCompletedEvent(Id, SKU, fromLocationId, toLocationId, quantity));
    }

    // ── Pricing ─────────────────────────────────────────────────────

    public void UpdatePricing(decimal unitPrice, decimal costPrice)
    {
        var previousPrice = UnitPrice;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PriceChangedEvent(Id, SKU, previousPrice, unitPrice));
    }

    // ── Lifecycle ───────────────────────────────────────────────────

    public void Discontinue()
    {
        IsActive = false;
        StockStatus = StockStatus.Discontinued;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductDiscontinuedEvent(Id, SKU, Name));
    }

    public void Update(
        string name,
        string? description,
        ProductCategory category,
        int minimumStockLevel,
        int maximumStockLevel,
        string? barcode,
        string unit,
        string currency,
        DateTime? expiryDate)
    {
        Name = name;
        Description = description;
        Category = category;
        MinimumStockLevel = minimumStockLevel;
        MaximumStockLevel = maximumStockLevel;
        Barcode = barcode;
        Unit = unit;
        Currency = currency;
        ExpiryDate = expiryDate;
        UpdatedAt = DateTime.UtcNow;

        RecalculateStockStatus();
    }

    public void SetLocation(Guid? locationId)
    {
        LocationId = locationId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSupplier(Guid? supplierId)
    {
        SupplierId = supplierId;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Status Calculation ──────────────────────────────────────────

    private void RecalculateStockStatus()
    {
        if (!IsActive)
        {
            StockStatus = StockStatus.Discontinued;
            return;
        }

        StockStatus = CurrentStock switch
        {
            0 => StockStatus.OutOfStock,
            _ when CurrentStock <= MinimumStockLevel => StockStatus.LowStock,
            _ when CurrentStock >= MaximumStockLevel => StockStatus.Overstock,
            _ => StockStatus.InStock
        };
    }
}
