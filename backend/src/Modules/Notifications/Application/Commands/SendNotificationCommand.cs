using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain.Entities;

namespace IMS.Modular.Modules.Notifications.Application.Commands;

/// <summary>
/// US-066: Cria e persiste uma notificação para um usuário.
/// </summary>
public record SendNotificationCommand(
    Guid UserId,
    string Type,
    string Title,
    string Body);

public class SendNotificationHandler(INotificationRepository repo)
{
    public async Task<Notification> HandleAsync(SendNotificationCommand cmd, CancellationToken ct = default)
    {
        var notification = new Notification(cmd.UserId, cmd.Type, cmd.Title, cmd.Body);
        await repo.AddAsync(notification, ct);
        await repo.SaveChangesAsync(ct);
        return notification;
    }
}
