using IMS.Modular.Modules.Inventory.Domain.Events;
using IMS.Modular.Shared.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Inventory.Application.Handlers;

/// <summary>
/// US-046: MediatR notification handlers that persist Inventory domain events
/// into the Outbox so the OutboxProcessor can publish them to RabbitMQ.
/// </summary>

// ── Product Events ─────────────────────────────────────────────────────────

public sealed class ProductCreatedOutboxHandler(
    IOutboxService outbox,
    ILogger<ProductCreatedOutboxHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    public async Task Handle(ProductCreatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting ProductCreatedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.product_created",
            message: new
            {
                notification.ProductId,
                notification.Name,
                notification.SKU,
                Category = notification.Category.ToString(),
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class StockChangedOutboxHandler(
    IOutboxService outbox,
    ILogger<StockChangedOutboxHandler> logger)
    : INotificationHandler<StockChangedEvent>
{
    public async Task Handle(StockChangedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting StockChangedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.stock_changed",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.PreviousStock,
                notification.NewStock,
                MovementType = notification.MovementType.ToString(),
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class LowStockAlertOutboxHandler(
    IOutboxService outbox,
    ILogger<LowStockAlertOutboxHandler> logger)
    : INotificationHandler<LowStockAlertEvent>
{
    public async Task Handle(LowStockAlertEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting LowStockAlertEvent for Product={ProductId} SKU={SKU}",
            notification.ProductId, notification.SKU);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.low_stock_alert",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.Name,
                notification.CurrentStock,
                notification.MinimumStockLevel,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class OutOfStockOutboxHandler(
    IOutboxService outbox,
    ILogger<OutOfStockOutboxHandler> logger)
    : INotificationHandler<OutOfStockEvent>
{
    public async Task Handle(OutOfStockEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting OutOfStockEvent for Product={ProductId} SKU={SKU}",
            notification.ProductId, notification.SKU);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.out_of_stock",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.Name,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class StockReplenishedOutboxHandler(
    IOutboxService outbox,
    ILogger<StockReplenishedOutboxHandler> logger)
    : INotificationHandler<StockReplenishedEvent>
{
    public async Task Handle(StockReplenishedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting StockReplenishedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.stock_replenished",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.PreviousStock,
                notification.NewStock,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class ProductDiscontinuedOutboxHandler(
    IOutboxService outbox,
    ILogger<ProductDiscontinuedOutboxHandler> logger)
    : INotificationHandler<ProductDiscontinuedEvent>
{
    public async Task Handle(ProductDiscontinuedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting ProductDiscontinuedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.product_discontinued",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.Name,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class PriceChangedOutboxHandler(
    IOutboxService outbox,
    ILogger<PriceChangedOutboxHandler> logger)
    : INotificationHandler<PriceChangedEvent>
{
    public async Task Handle(PriceChangedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting PriceChangedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.price_changed",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.PreviousPrice,
                notification.NewPrice,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class StockTransferInitiatedOutboxHandler(
    IOutboxService outbox,
    ILogger<StockTransferInitiatedOutboxHandler> logger)
    : INotificationHandler<StockTransferInitiatedEvent>
{
    public async Task Handle(StockTransferInitiatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting StockTransferInitiatedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.stock_transfer_initiated",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.FromLocationId,
                notification.ToLocationId,
                notification.Quantity,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class StockTransferCompletedOutboxHandler(
    IOutboxService outbox,
    ILogger<StockTransferCompletedOutboxHandler> logger)
    : INotificationHandler<StockTransferCompletedEvent>
{
    public async Task Handle(StockTransferCompletedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting StockTransferCompletedEvent for Product={ProductId}", notification.ProductId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.stock_transfer_completed",
            message: new
            {
                notification.ProductId,
                notification.SKU,
                notification.FromLocationId,
                notification.ToLocationId,
                notification.Quantity,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

// ── Supplier Events ────────────────────────────────────────────────────────

public sealed class SupplierCreatedOutboxHandler(
    IOutboxService outbox,
    ILogger<SupplierCreatedOutboxHandler> logger)
    : INotificationHandler<SupplierCreatedEvent>
{
    public async Task Handle(SupplierCreatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting SupplierCreatedEvent for Supplier={SupplierId}", notification.SupplierId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.supplier_created",
            message: new
            {
                notification.SupplierId,
                notification.Name,
                notification.Code,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class SupplierDeactivatedOutboxHandler(
    IOutboxService outbox,
    ILogger<SupplierDeactivatedOutboxHandler> logger)
    : INotificationHandler<SupplierDeactivatedEvent>
{
    public async Task Handle(SupplierDeactivatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting SupplierDeactivatedEvent for Supplier={SupplierId}", notification.SupplierId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.supplier_deactivated",
            message: new
            {
                notification.SupplierId,
                notification.Name,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

// ── Location Events ────────────────────────────────────────────────────────

public sealed class LocationCreatedOutboxHandler(
    IOutboxService outbox,
    ILogger<LocationCreatedOutboxHandler> logger)
    : INotificationHandler<LocationCreatedEvent>
{
    public async Task Handle(LocationCreatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting LocationCreatedEvent for Location={LocationId}", notification.LocationId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.location_created",
            message: new
            {
                notification.LocationId,
                notification.Name,
                notification.Code,
                Type = notification.Type.ToString(),
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class LocationDeactivatedOutboxHandler(
    IOutboxService outbox,
    ILogger<LocationDeactivatedOutboxHandler> logger)
    : INotificationHandler<LocationDeactivatedEvent>
{
    public async Task Handle(LocationDeactivatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting LocationDeactivatedEvent for Location={LocationId}", notification.LocationId);
        await outbox.SaveAsync(
            exchange: "ims.inventory",
            routingKey: "inventory.location_deactivated",
            message: new
            {
                notification.LocationId,
                notification.Name,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}
