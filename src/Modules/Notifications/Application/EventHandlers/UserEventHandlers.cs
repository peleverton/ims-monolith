using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain;
using IMS.Modular.Modules.UserManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Notifications.Application.EventHandlers;

// ── US-021: Domain Event → Notification Handlers ──────────────────────────────

/// <summary>
/// When a user is activated, send a real-time notification to admins
/// and notify the user via email.
/// </summary>
public class UserActivatedNotificationHandler(
    INotificationService notificationService,
    ILogger<UserActivatedNotificationHandler> logger)
    : INotificationHandler<UserActivatedEvent>
{
    public async Task Handle(UserActivatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("User {UserId} activated — sending notifications", notification.UserId);

        // SignalR: notify admins group
        await notificationService.SendToGroupAsync("admins", new NotificationPayload(
            Type: NotificationType.UserActivated,
            Title: "User Activated",
            Message: $"User {notification.Email} has been activated.",
            Metadata: new Dictionary<string, string> { ["userId"] = notification.UserId.ToString() }
        ), ct);
    }
}

/// <summary>
/// When a user is deactivated, send a real-time notification to admins.
/// </summary>
public class UserDeactivatedNotificationHandler(
    INotificationService notificationService,
    ILogger<UserDeactivatedNotificationHandler> logger)
    : INotificationHandler<UserDeactivatedEvent>
{
    public async Task Handle(UserDeactivatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("User {UserId} deactivated — sending notifications", notification.UserId);

        await notificationService.SendToGroupAsync("admins", new NotificationPayload(
            Type: NotificationType.UserDeactivated,
            Title: "User Deactivated",
            Message: $"User {notification.Email} has been deactivated.",
            Metadata: new Dictionary<string, string> { ["userId"] = notification.UserId.ToString() }
        ), ct);
    }
}

/// <summary>
/// When a user's role is assigned or removed, notify admins.
/// </summary>
public class UserRoleAssignedNotificationHandler(
    INotificationService notificationService,
    ILogger<UserRoleAssignedNotificationHandler> logger)
    : INotificationHandler<UserRoleAssignedEvent>
{
    public async Task Handle(UserRoleAssignedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Role {RoleName} assigned to user {UserId}", notification.RoleName, notification.UserId);

        await notificationService.SendToGroupAsync("admins", new NotificationPayload(
            Type: NotificationType.UserRoleChanged,
            Title: "Role Assigned",
            Message: $"Role '{notification.RoleName}' was assigned to user {notification.UserId}.",
            Metadata: new Dictionary<string, string>
            {
                ["userId"] = notification.UserId.ToString(),
                ["roleId"] = notification.RoleId.ToString(),
                ["roleName"] = notification.RoleName
            }
        ), ct);
    }
}

public class UserRoleRemovedNotificationHandler(
    INotificationService notificationService,
    ILogger<UserRoleRemovedNotificationHandler> logger)
    : INotificationHandler<UserRoleRemovedEvent>
{
    public async Task Handle(UserRoleRemovedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Role {RoleName} removed from user {UserId}", notification.RoleName, notification.UserId);

        await notificationService.SendToGroupAsync("admins", new NotificationPayload(
            Type: NotificationType.UserRoleChanged,
            Title: "Role Removed",
            Message: $"Role '{notification.RoleName}' was removed from user {notification.UserId}.",
            Metadata: new Dictionary<string, string>
            {
                ["userId"] = notification.UserId.ToString(),
                ["roleId"] = notification.RoleId.ToString(),
                ["roleName"] = notification.RoleName
            }
        ), ct);
    }
}

/// <summary>
/// When a user's profile is updated, send notification to the user.
/// </summary>
public class UserProfileUpdatedNotificationHandler(
    INotificationService notificationService,
    ILogger<UserProfileUpdatedNotificationHandler> logger)
    : INotificationHandler<UserProfileUpdatedEvent>
{
    public async Task Handle(UserProfileUpdatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Profile updated for user {UserId}", notification.UserId);

        await notificationService.SendToUserAsync(notification.UserId.ToString(), new NotificationPayload(
            Type: NotificationType.SystemAlert,
            Title: "Profile Updated",
            Message: "Your profile has been successfully updated.",
            Metadata: new Dictionary<string, string> { ["userId"] = notification.UserId.ToString() }
        ), ct);
    }
}

/// <summary>
/// When a user's password changes, send email confirmation.
/// </summary>
public class UserPasswordChangedNotificationHandler(
    IEmailService emailService,
    ILogger<UserPasswordChangedNotificationHandler> logger)
    : INotificationHandler<UserPasswordChangedEvent>
{
    public async Task Handle(UserPasswordChangedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Password changed for user {UserId}", notification.UserId);

        await emailService.SendAsync(new EmailMessage(
            To: notification.Email,
            Subject: "IMS — Password Changed",
            Body: $"""
                <h2>Password Changed</h2>
                <p>Your password was successfully changed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.</p>
                <p>If you did not perform this action, please contact your administrator immediately.</p>
                """,
            IsHtml: true
        ), ct);
    }
}
