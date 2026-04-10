using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.InventoryIssues.Domain.Events;

public sealed class InventoryIssueCreatedEvent(
    Guid issueId, InventoryIssueType type, InventoryIssuePriority priority, Guid reporterId) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public InventoryIssueType Type { get; } = type;
    public InventoryIssuePriority Priority { get; } = priority;
    public Guid ReporterId { get; } = reporterId;
}

public sealed class InventoryIssueAssignedEvent(
    Guid issueId, Guid? oldAssigneeId, Guid newAssigneeId, Guid userId) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public Guid? OldAssigneeId { get; } = oldAssigneeId;
    public Guid NewAssigneeId { get; } = newAssigneeId;
    public Guid UserId { get; } = userId;
}

public sealed class InventoryIssueStatusChangedEvent(
    Guid issueId, InventoryIssueStatus oldStatus, InventoryIssueStatus newStatus, Guid userId) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public InventoryIssueStatus OldStatus { get; } = oldStatus;
    public InventoryIssueStatus NewStatus { get; } = newStatus;
    public Guid UserId { get; } = userId;
}

public sealed class InventoryIssueResolvedEvent(
    Guid issueId, Guid userId, DateTime createdAt) : DomainEventBase
{
    public Guid IssueId { get; } = issueId;
    public Guid UserId { get; } = userId;
    public DateTime CreatedAt { get; } = createdAt;
}
