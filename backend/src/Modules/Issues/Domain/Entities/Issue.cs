using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Modules.Issues.Domain.ValueObjects;

namespace IMS.Modular.Modules.Issues.Domain.Entities;

public class Issue : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public IssueStatus Status { get; private set; }
    public IssuePriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid ReporterId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public List<IssueComment> Comments { get; private set; } = [];
    public List<IssueActivity> Activities { get; private set; } = [];
    public List<IssueTag> Tags { get; private set; } = [];

    private Issue() { }

    public Issue(string title, string description, IssuePriority priority, Guid reporterId, DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = description;
        Status = IssueStatus.Open;
        Priority = priority;
        ReporterId = reporterId;
        DueDate = dueDate;

        AddActivity(IssueActivityType.Created, reporterId, "Issue created");
        AddDomainEvent(new IssueCreatedEvent(Id, title, priority, reporterId));
    }

    public void UpdateTitle(string title, Guid userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var oldTitle = Title;
        Title = title;
        AddActivity(IssueActivityType.Updated, userId, $"Title updated from '{oldTitle}' to '{title}'");
    }

    public void UpdateDescription(string description, Guid userId)
    {
        Description = description;
        AddActivity(IssueActivityType.Updated, userId, "Description updated");
    }

    public void UpdateStatus(IssueStatus status, Guid userId)
    {
        var oldStatus = Status;
        Status = status;
        AddActivity(IssueActivityType.StatusChanged, userId, $"Status changed from {oldStatus} to {status}");
        AddDomainEvent(new IssueStatusChangedEvent(Id, oldStatus, status, userId));

        if (status is IssueStatus.Closed or IssueStatus.Resolved)
        {
            ResolvedAt = DateTime.UtcNow;
            AddDomainEvent(new IssueCompletedEvent(Id, userId, CreatedAt));
        }
    }

    public void AssignTo(Guid assigneeId, Guid userId)
    {
        var oldAssigneeId = AssigneeId;
        AssigneeId = assigneeId;
        AddActivity(IssueActivityType.Assigned, userId, $"Issue assigned to user {assigneeId}");
        AddDomainEvent(new IssueAssignedEvent(Id, oldAssigneeId, assigneeId, userId));
    }

    public void Unassign(Guid userId)
    {
        AssigneeId = null;
        AddActivity(IssueActivityType.Unassigned, userId, "Issue unassigned");
    }

    public void UpdatePriority(IssuePriority priority, Guid userId)
    {
        var oldPriority = Priority;
        Priority = priority;
        AddActivity(IssueActivityType.Updated, userId, $"Priority changed from {oldPriority} to {priority}");
    }

    public void UpdateDueDate(DateTime? dueDate, Guid userId)
    {
        DueDate = dueDate;
        var dueDateText = dueDate?.ToString("yyyy-MM-dd") ?? "no date";
        AddActivity(IssueActivityType.Updated, userId, $"Due date updated to {dueDateText}");
    }

    public void AddComment(string content, Guid userId)
    {
        var comment = new IssueComment(Id, content, userId);
        Comments.Add(comment);
        AddActivity(IssueActivityType.CommentAdded, userId, "Comment added");
        AddDomainEvent(new IssueCommentAddedEvent(Id, comment.Id, userId, content));
    }

    public void AddTag(string name, string color, Guid userId)
    {
        Tags.Add(new IssueTag(name, color));
        AddActivity(IssueActivityType.TagAdded, userId, $"Tag '{name}' added");
    }

    public void RemoveTag(string tagName, Guid userId)
    {
        var tag = Tags.FirstOrDefault(t => t.Name == tagName);
        if (tag is not null)
        {
            Tags.Remove(tag);
            AddActivity(IssueActivityType.TagRemoved, userId, $"Tag '{tagName}' removed");
        }
    }

    private void AddActivity(IssueActivityType activityType, Guid userId, string description)
    {
        Activities.Add(new IssueActivity(Id, activityType, userId, description));
    }
}
