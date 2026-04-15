using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;

namespace IMS.Modular.Modules.Notifications.Infrastructure;

// ── SignalR Notification Service ──────────────────────────────────────────────

public class NotificationService(IHubContext<NotificationHub> hubContext)
    : INotificationService
{
    public async Task SendToUserAsync(string userId, NotificationPayload payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"user-{userId}")
            .SendAsync("ReceiveNotification", payload, ct);

    public async Task SendToUsersAsync(
        IEnumerable<string> userIds,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        foreach (var userId in userIds)
            await SendToUserAsync(userId, payload, ct);
    }

    public async Task SendToAllAsync(NotificationPayload payload, CancellationToken ct = default)
        => await hubContext.Clients.All.SendAsync("ReceiveNotification", payload, ct);

    public async Task SendToGroupAsync(string groupName, NotificationPayload payload, CancellationToken ct = default)
        => await hubContext.Clients.Group(groupName)
            .SendAsync("ReceiveNotification", payload, ct);
}
