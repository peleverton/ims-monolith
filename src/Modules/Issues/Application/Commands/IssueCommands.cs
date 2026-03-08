using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.Issues.Application.Commands;

public record CreateIssueCommand(
    string Title, string Description, IssuePriority Priority,
    Guid ReporterId, DateTime? DueDate) : IRequest<Result<IssueDto>>;

public record UpdateIssueCommand(
    Guid Id, string? Title, string? Description,
    IssuePriority? Priority, DateTime? DueDate, Guid UserId) : IRequest<Result<IssueDto>>;

public record ChangeIssueStatusCommand(
    Guid Id, IssueStatus Status, Guid UserId) : IRequest<Result<IssueDto>>;

public record AssignIssueCommand(
    Guid Id, Guid AssigneeId, Guid UserId) : IRequest<Result<IssueDto>>;

public record DeleteIssueCommand(Guid Id) : IRequest<Result<bool>>;

public record AddCommentCommand(
    Guid IssueId, string Content, Guid UserId) : IRequest<Result<IssueDto>>;

public record AddTagCommand(
    Guid IssueId, string Name, string Color, Guid UserId) : IRequest<Result<IssueDto>>;
