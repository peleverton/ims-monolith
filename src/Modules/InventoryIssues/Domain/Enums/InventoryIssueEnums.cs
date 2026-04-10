namespace IMS.Modular.Modules.InventoryIssues.Domain.Enums;

public enum InventoryIssueStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    Reopened
}

public enum InventoryIssuePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum InventoryIssueType
{
    Damage,
    Loss,
    Discrepancy,
    Expiry,
    Quality,
    Shortage,
    Overflow,
    Misplacement,
    CountError,
    SystemError,
    SupplierIssue,
    Other
}
