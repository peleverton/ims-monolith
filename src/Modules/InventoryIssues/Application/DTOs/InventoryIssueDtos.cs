using IMS.Modular.Modules.InventoryIssues.Domain.Enums;

namespace IMS.Modular.Modules.InventoryIssues.Application.DTOs;

public record CreateInventoryIssueRequest(
    string Title,
    string Description,
    InventoryIssueType Type,
    InventoryIssuePriority Priority,
    Guid ReporterId,
    Guid? ProductId = null,
    Guid? LocationId = null,
    int? AffectedQuantity = null,
    decimal? EstimatedLoss = null,
    DateTime? DueDate = null);

public record UpdateInventoryIssueRequest(
    string Title,
    string Description,
    InventoryIssueType Type,
    InventoryIssuePriority Priority,
    Guid? ProductId,
    Guid? LocationId,
    int? AffectedQuantity,
    decimal? EstimatedLoss,
    DateTime? DueDate);

public record AssignInventoryIssueRequest(Guid AssigneeId);

public record ResolveInventoryIssueRequest(string? ResolutionNotes);
