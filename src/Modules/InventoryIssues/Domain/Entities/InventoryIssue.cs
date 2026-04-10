using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Modules.InventoryIssues.Domain.Events;
using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.InventoryIssues.Domain.Entities;

/// <summary>
/// InventoryIssue — Aggregate Root for inventory-specific problem tracking.
/// Lifecycle: Open → InProgress → Resolved/Closed → Reopened
/// </summary>
public class InventoryIssue : BaseEntity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public InventoryIssueType Type { get; private set; }
    public InventoryIssuePriority Priority { get; private set; }
    public InventoryIssueStatus Status { get; private set; }

    public Guid? ProductId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid? AssigneeId { get; private set; }

    public int? AffectedQuantity { get; private set; }
    public decimal? EstimatedLoss { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private InventoryIssue() { }

    public InventoryIssue(
        string title,
        string description,
        InventoryIssueType type,
        InventoryIssuePriority priority,
        Guid reporterId,
        Guid? productId = null,
        Guid? locationId = null,
        int? affectedQuantity = null,
        decimal? estimatedLoss = null,
        DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Title = title;
        Description = description;
        Type = type;
        Priority = priority;
        Status = InventoryIssueStatus.Open;
        ReporterId = reporterId;
        ProductId = productId;
        LocationId = locationId;
        AffectedQuantity = affectedQuantity;
        EstimatedLoss = estimatedLoss;
        DueDate = dueDate;

        AddDomainEvent(new InventoryIssueCreatedEvent(Id, type, priority, reporterId));
    }

    public void Update(
        string title,
        string description,
        InventoryIssueType type,
        InventoryIssuePriority priority,
        Guid? productId,
        Guid? locationId,
        int? affectedQuantity,
        decimal? estimatedLoss,
        DateTime? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Type = type;
        Priority = priority;
        ProductId = productId;
        LocationId = locationId;
        AffectedQuantity = affectedQuantity;
        EstimatedLoss = estimatedLoss;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Assign(Guid assigneeId, Guid userId)
    {
        var old = AssigneeId;
        AssigneeId = assigneeId;
        if (Status == InventoryIssueStatus.Open)
            Status = InventoryIssueStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new InventoryIssueAssignedEvent(Id, old, assigneeId, userId));
    }

    public void Resolve(string? resolutionNotes, Guid userId)
    {
        Status = InventoryIssueStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new InventoryIssueStatusChangedEvent(Id, InventoryIssueStatus.InProgress, InventoryIssueStatus.Resolved, userId));
        AddDomainEvent(new InventoryIssueResolvedEvent(Id, userId, CreatedAt));
    }

    public void Close(Guid userId)
    {
        var old = Status;
        Status = InventoryIssueStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new InventoryIssueStatusChangedEvent(Id, old, InventoryIssueStatus.Closed, userId));
    }

    public void Reopen(Guid userId)
    {
        var old = Status;
        Status = InventoryIssueStatus.Reopened;
        ResolvedAt = null;
        ResolutionNotes = null;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new InventoryIssueStatusChangedEvent(Id, old, InventoryIssueStatus.Reopened, userId));
    }

    public void UpdatePriority(InventoryIssuePriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }
}
