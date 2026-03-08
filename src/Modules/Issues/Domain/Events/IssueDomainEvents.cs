using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Issues.Domain.Enums;

namespace IMS.Modular.Modules.Issues.Domain.Events;

public sealed class IssueCreatedEvent(
    Guid issueId, string title, IssuePriority priority, Guid reporterId) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public string Title { get; } = title;
    public IssuePriority Priority { get; } = priority;
    public Guid ReporterId { get; } = reporterId;
}

public sealed class IssueStatusChangedEvent(
    Guid issueId, IssueStatus oldStatus, IssueStatus newStatus, Guid changedBy) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public IssueStatus OldStatus { get; } = oldStatus;
    public IssueStatus NewStatus { get; } = newStatus;
    public Guid ChangedBy { get; } = changedBy;
}

public sealed class IssueAssignedEvent(
    Guid issueId, Guid? previousAssigneeId, Guid newAssigneeId, Guid assignedBy) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public Guid? PreviousAssigneeId { get; } = previousAssigneeId;
    public Guid NewAssigneeId { get; } = newAssigneeId;
    public Guid AssignedBy { get; } = assignedBy;
}

public sealed class IssueCompletedEvent(
    Guid issueId, Guid completedBy, DateTime createdAt) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public Guid CompletedBy { get; } = completedBy;
    public TimeSpan Duration { get; } = DateTime.UtcNow - createdAt;
}

public sealed class IssueCommentAddedEvent(
    Guid issueId, Guid commentId, Guid userId, string content) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public Guid CommentId { get; } = commentId;
    public Guid UserId { get; } = userId;
    public string Content { get; } = content;
}
