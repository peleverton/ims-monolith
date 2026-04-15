using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using MediatR;

namespace IMS.Modular.Modules.InventoryIssues.Application.Commands;

public record CreateInventoryIssueCommand(
    string Title,
    string Description,
    InventoryIssueType Type,
    InventoryIssuePriority Priority,
    Guid ReporterId,
    Guid? ProductId,
    Guid? LocationId,
    int? AffectedQuantity,
    decimal? EstimatedLoss,
    DateTime? DueDate) : IRequest<Guid>;

public record UpdateInventoryIssueCommand(
    Guid Id,
    string Title,
    string Description,
    InventoryIssueType Type,
    InventoryIssuePriority Priority,
    Guid? ProductId,
    Guid? LocationId,
    int? AffectedQuantity,
    decimal? EstimatedLoss,
    DateTime? DueDate) : IRequest<bool>;

public record AssignInventoryIssueCommand(
    Guid Id,
    Guid AssigneeId,
    Guid UserId) : IRequest<bool>;

public record ResolveInventoryIssueCommand(
    Guid Id,
    string? ResolutionNotes,
    Guid UserId) : IRequest<bool>;

public record CloseInventoryIssueCommand(
    Guid Id,
    Guid UserId) : IRequest<bool>;

public record ReopenInventoryIssueCommand(
    Guid Id,
    Guid UserId) : IRequest<bool>;

public record DeleteInventoryIssueCommand(Guid Id) : IRequest<bool>;
