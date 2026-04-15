using IMS.Modular.Modules.Notifications.Domain;

namespace IMS.Modular.Modules.Notifications.Application;

// ── Notification service interface ────────────────────────────────────────────

public interface INotificationService
{
    /// <summary>Send a real-time notification to a specific user (SignalR).</summary>
    Task SendToUserAsync(string userId, NotificationPayload payload, CancellationToken ct = default);

    /// <summary>Send a real-time notification to multiple users (SignalR).</summary>
    Task SendToUsersAsync(IEnumerable<string> userIds, NotificationPayload payload, CancellationToken ct = default);

    /// <summary>Broadcast a notification to all connected clients (SignalR).</summary>
    Task SendToAllAsync(NotificationPayload payload, CancellationToken ct = default);

    /// <summary>Send a notification to a named group (SignalR).</summary>
    Task SendToGroupAsync(string groupName, NotificationPayload payload, CancellationToken ct = default);
}

// ── Email service interface ───────────────────────────────────────────────────

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
    Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default);
}

// ── Message bus interface ─────────────────────────────────────────────────────

public interface IMessageBusService
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class;
    Task PublishToExchangeAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default) where T : class;
}
