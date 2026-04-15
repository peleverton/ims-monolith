namespace IMS.Modular.Modules.Issues.Domain.Enums;

public enum IssueStatus
{
    Open,
    InProgress,
    Testing,
    Resolved,
    Closed
}

public enum IssuePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum IssueActivityType
{
    Created,
    Updated,
    StatusChanged,
    Assigned,
    Unassigned,
    CommentAdded,
    TagAdded,
    TagRemoved,
    MetadataUpdated
}
