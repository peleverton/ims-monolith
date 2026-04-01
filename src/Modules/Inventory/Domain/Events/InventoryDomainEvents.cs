using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Modules.Inventory.Domain.Events;

// ── Product Events ─────────────────────────────────────────────────────

public sealed class ProductCreatedEvent(
    Guid productId, string name, string sku, ProductCategory category) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string Name { get; } = name;
    public string SKU { get; } = sku;
    public ProductCategory Category { get; } = category;
}

public sealed class StockChangedEvent(
    Guid productId, string sku, int previousStock, int newStock, StockMovementType movementType) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public int PreviousStock { get; } = previousStock;
    public int NewStock { get; } = newStock;
    public StockMovementType MovementType { get; } = movementType;
}

public sealed class LowStockAlertEvent(
    Guid productId, string sku, string name, int currentStock, int minimumStockLevel) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public string Name { get; } = name;
    public int CurrentStock { get; } = currentStock;
    public int MinimumStockLevel { get; } = minimumStockLevel;
}

public sealed class OutOfStockEvent(
    Guid productId, string sku, string name) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public string Name { get; } = name;
}

public sealed class StockReplenishedEvent(
    Guid productId, string sku, int previousStock, int newStock) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public int PreviousStock { get; } = previousStock;
    public int NewStock { get; } = newStock;
}

public sealed class ProductDiscontinuedEvent(
    Guid productId, string sku, string name) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public string Name { get; } = name;
}

public sealed class PriceChangedEvent(
    Guid productId, string sku, decimal previousPrice, decimal newPrice) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public decimal PreviousPrice { get; } = previousPrice;
    public decimal NewPrice { get; } = newPrice;
}

public sealed class StockTransferInitiatedEvent(
    Guid productId, string sku, Guid? fromLocationId, Guid? toLocationId, int quantity) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public Guid? FromLocationId { get; } = fromLocationId;
    public Guid? ToLocationId { get; } = toLocationId;
    public int Quantity { get; } = quantity;
}

public sealed class StockTransferCompletedEvent(
    Guid productId, string sku, Guid? fromLocationId, Guid? toLocationId, int quantity) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public Guid? FromLocationId { get; } = fromLocationId;
    public Guid? ToLocationId { get; } = toLocationId;
    public int Quantity { get; } = quantity;
}

public sealed class ProductExpiringSoonEvent(
    Guid productId, string sku, string name, DateTime expiryDate) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public string Name { get; } = name;
    public DateTime ExpiryDate { get; } = expiryDate;
}

public sealed class ProductExpiredEvent(
    Guid productId, string sku, string name, DateTime expiryDate) : DomainEventBase
{
    public Guid ProductId { get; } = productId;
    public string SKU { get; } = sku;
    public string Name { get; } = name;
    public DateTime ExpiryDate { get; } = expiryDate;
}

// ── Supplier Events ────────────────────────────────────────────────────

public sealed class SupplierCreatedEvent(
    Guid supplierId, string name, string code) : DomainEventBase
{
    public Guid SupplierId { get; } = supplierId;
    public string Name { get; } = name;
    public string Code { get; } = code;
}

public sealed class SupplierDeactivatedEvent(
    Guid supplierId, string name) : DomainEventBase
{
    public Guid SupplierId { get; } = supplierId;
    public string Name { get; } = name;
}

// ── Location Events ────────────────────────────────────────────────────

public sealed class LocationCreatedEvent(
    Guid locationId, string name, string code, LocationType type) : DomainEventBase
{
    public Guid LocationId { get; } = locationId;
    public string Name { get; } = name;
    public string Code { get; } = code;
    public LocationType Type { get; } = type;
}

public sealed class LocationDeactivatedEvent(
    Guid locationId, string name) : DomainEventBase
{
    public Guid LocationId { get; } = locationId;
    public string Name { get; } = name;
}
