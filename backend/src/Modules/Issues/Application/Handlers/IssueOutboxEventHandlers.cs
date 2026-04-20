using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Shared.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Issues.Application.Handlers;

/// <summary>
/// US-045: MediatR notification handlers that persist Issue domain events
/// into the Outbox so the OutboxProcessor can publish them to RabbitMQ.
/// </summary>

public sealed class IssueCreatedOutboxHandler(
    IOutboxService outbox,
    ILogger<IssueCreatedOutboxHandler> logger)
    : INotificationHandler<IssueCreatedEvent>
{
    public async Task Handle(IssueCreatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting IssueCreatedEvent for Issue={IssueId}", notification.IssueId);
        await outbox.SaveAsync(
            exchange: "ims.issues",
            routingKey: "issue.created",
            message: new
            {
                notification.IssueId,
                notification.Title,
                Priority = notification.Priority.ToString(),
                notification.ReporterId,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class IssueStatusChangedOutboxHandler(
    IOutboxService outbox,
    ILogger<IssueStatusChangedOutboxHandler> logger)
    : INotificationHandler<IssueStatusChangedEvent>
{
    public async Task Handle(IssueStatusChangedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting IssueStatusChangedEvent for Issue={IssueId}", notification.IssueId);
        await outbox.SaveAsync(
            exchange: "ims.issues",
            routingKey: "issue.status_changed",
            message: new
            {
                notification.IssueId,
                OldStatus = notification.OldStatus.ToString(),
                NewStatus = notification.NewStatus.ToString(),
                notification.ChangedBy,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class IssueAssignedOutboxHandler(
    IOutboxService outbox,
    ILogger<IssueAssignedOutboxHandler> logger)
    : INotificationHandler<IssueAssignedEvent>
{
    public async Task Handle(IssueAssignedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting IssueAssignedEvent for Issue={IssueId}", notification.IssueId);
        await outbox.SaveAsync(
            exchange: "ims.issues",
            routingKey: "issue.assigned",
            message: new
            {
                notification.IssueId,
                notification.PreviousAssigneeId,
                notification.NewAssigneeId,
                notification.AssignedBy,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class IssueCompletedOutboxHandler(
    IOutboxService outbox,
    ILogger<IssueCompletedOutboxHandler> logger)
    : INotificationHandler<IssueCompletedEvent>
{
    public async Task Handle(IssueCompletedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting IssueCompletedEvent for Issue={IssueId}", notification.IssueId);
        await outbox.SaveAsync(
            exchange: "ims.issues",
            routingKey: "issue.completed",
            message: new
            {
                notification.IssueId,
                notification.CompletedBy,
                DurationSeconds = (long)notification.Duration.TotalSeconds,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}

public sealed class IssueCommentAddedOutboxHandler(
    IOutboxService outbox,
    ILogger<IssueCommentAddedOutboxHandler> logger)
    : INotificationHandler<IssueCommentAddedEvent>
{
    public async Task Handle(IssueCommentAddedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[Outbox] Persisting IssueCommentAddedEvent for Issue={IssueId}", notification.IssueId);
        await outbox.SaveAsync(
            exchange: "ims.issues",
            routingKey: "issue.comment_added",
            message: new
            {
                notification.IssueId,
                notification.CommentId,
                notification.UserId,
                // Truncate content to avoid large messages
                ContentPreview = notification.Content.Length > 200
                    ? notification.Content[..200] + "…"
                    : notification.Content,
                OccurredOn = notification.OccurredOn
            },
            cancellationToken: ct);
    }
}
