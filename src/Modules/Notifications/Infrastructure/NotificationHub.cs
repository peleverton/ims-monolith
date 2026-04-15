using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IMS.Modular.Modules.Notifications.Infrastructure;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client can join a named group (e.g. "admins", "managers").</summary>
    public async Task JoinGroup(string groupName)
        => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    /// <summary>Client leaves a named group.</summary>
    public async Task LeaveGroup(string groupName)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}
