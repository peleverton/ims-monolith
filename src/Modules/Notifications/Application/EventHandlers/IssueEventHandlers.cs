using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Notifications.Application.EventHandlers;

// ── US-021: Issue Domain Events → Notifications ───────────────────────────────

public class IssueAssignedNotificationHandler(
    INotificationService notificationService,
    ILogger<IssueAssignedNotificationHandler> logger)
    : INotificationHandler<IssueAssignedEvent>
{
    public async Task Handle(IssueAssignedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Issue {IssueId} assigned to user {AssigneeId}", notification.IssueId, notification.NewAssigneeId);

        // Notify the new assignee
        await notificationService.SendToUserAsync(
            notification.NewAssigneeId.ToString(),
            new NotificationPayload(
                Type: NotificationType.IssueAssigned,
                Title: "Issue Assigned to You",
                Message: $"You have been assigned to issue #{notification.IssueId}.",
                ActionUrl: $"/issues/{notification.IssueId}",
                UserId: notification.NewAssigneeId.ToString(),
                Metadata: new Dictionary<string, string>
                {
                    ["issueId"] = notification.IssueId.ToString(),
                    ["assignedBy"] = notification.AssignedBy.ToString()
                }
            ), ct);

        // Notify previous assignee if there was one
        if (notification.PreviousAssigneeId.HasValue)
        {
            await notificationService.SendToUserAsync(
                notification.PreviousAssigneeId.Value.ToString(),
                new NotificationPayload(
                    Type: NotificationType.IssueAssigned,
                    Title: "Issue Reassigned",
                    Message: $"Issue #{notification.IssueId} has been reassigned to another user.",
                    ActionUrl: $"/issues/{notification.IssueId}",
                    Metadata: new Dictionary<string, string>
                    {
                        ["issueId"] = notification.IssueId.ToString()
                    }
                ), ct);
        }
    }
}

public class IssueStatusChangedNotificationHandler(
    INotificationService notificationService,
    ILogger<IssueStatusChangedNotificationHandler> logger)
    : INotificationHandler<IssueStatusChangedEvent>
{
    public async Task Handle(IssueStatusChangedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Issue {IssueId} status changed from {Old} to {New}",
            notification.IssueId, notification.OldStatus, notification.NewStatus);

        // Broadcast status change to all connected clients watching the issue
        await notificationService.SendToGroupAsync($"issue-{notification.IssueId}", new NotificationPayload(
            Type: NotificationType.IssueStatusChanged,
            Title: "Issue Status Updated",
            Message: $"Issue status changed: {notification.OldStatus} → {notification.NewStatus}.",
            ActionUrl: $"/issues/{notification.IssueId}",
            Metadata: new Dictionary<string, string>
            {
                ["issueId"] = notification.IssueId.ToString(),
                ["oldStatus"] = notification.OldStatus.ToString(),
                ["newStatus"] = notification.NewStatus.ToString(),
                ["changedBy"] = notification.ChangedBy.ToString()
            }
        ), ct);
    }
}

public class IssueCommentAddedNotificationHandler(
    INotificationService notificationService,
    ILogger<IssueCommentAddedNotificationHandler> logger)
    : INotificationHandler<IssueCommentAddedEvent>
{
    public async Task Handle(IssueCommentAddedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Comment {CommentId} added to issue {IssueId} by user {UserId}",
            notification.CommentId, notification.IssueId, notification.UserId);

        await notificationService.SendToGroupAsync($"issue-{notification.IssueId}", new NotificationPayload(
            Type: NotificationType.IssueCommentAdded,
            Title: "New Comment",
            Message: "A new comment was added to the issue.",
            ActionUrl: $"/issues/{notification.IssueId}#comments",
            Metadata: new Dictionary<string, string>
            {
                ["issueId"] = notification.IssueId.ToString(),
                ["commentId"] = notification.CommentId.ToString(),
                ["userId"] = notification.UserId.ToString()
            }
        ), ct);
    }
}
