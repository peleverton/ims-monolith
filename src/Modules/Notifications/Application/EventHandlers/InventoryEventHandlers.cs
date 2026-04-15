using IMS.Modular.Modules.Inventory.Domain.Events;
using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Notifications.Application.EventHandlers;

// ── US-021: Inventory Domain Events → Notifications ───────────────────────────

public class LowStockAlertNotificationHandler(
    INotificationService notificationService,
    ILogger<LowStockAlertNotificationHandler> logger)
    : INotificationHandler<LowStockAlertEvent>
{
    public async Task Handle(LowStockAlertEvent notification, CancellationToken ct)
    {
        logger.LogWarning(
            "Low stock alert: product {SKU} ({Name}) has {CurrentStock}/{MinimumStockLevel}",
            notification.SKU, notification.Name, notification.CurrentStock, notification.MinimumStockLevel);

        // SignalR: notify inventory managers group
        await notificationService.SendToGroupAsync("inventory-managers", new NotificationPayload(
            Type: NotificationType.LowStockAlert,
            Title: "⚠️ Low Stock Alert",
            Message: $"Product {notification.Name} (SKU: {notification.SKU}) is running low. " +
                     $"Current: {notification.CurrentStock} / Minimum: {notification.MinimumStockLevel}.",
            ActionUrl: $"/inventory/products/{notification.ProductId}",
            Metadata: new Dictionary<string, string>
            {
                ["productId"] = notification.ProductId.ToString(),
                ["sku"] = notification.SKU,
                ["currentStock"] = notification.CurrentStock.ToString(),
                ["minimumStock"] = notification.MinimumStockLevel.ToString()
            }
        ), ct);

        // MessageBus: publish event for external consumers
        logger.LogInformation("[MessageBus][LowStock] product={SKU} stock={Stock}", notification.SKU, notification.CurrentStock);
    }
}

public class OutOfStockNotificationHandler(
    INotificationService notificationService,
    ILogger<OutOfStockNotificationHandler> logger)
    : INotificationHandler<OutOfStockEvent>
{
    public async Task Handle(OutOfStockEvent notification, CancellationToken ct)
    {
        logger.LogWarning("Out of stock: product {SKU} ({Name})", notification.SKU, notification.Name);

        await notificationService.SendToGroupAsync("inventory-managers", new NotificationPayload(
            Type: NotificationType.OutOfStockAlert,
            Title: "🚨 Out of Stock",
            Message: $"Product {notification.Name} (SKU: {notification.SKU}) is OUT OF STOCK.",
            ActionUrl: $"/inventory/products/{notification.ProductId}",
            Metadata: new Dictionary<string, string>
            {
                ["productId"] = notification.ProductId.ToString(),
                ["sku"] = notification.SKU
            }
        ), ct);
    }
}
