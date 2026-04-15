using IMS.Modular.Modules.Issues.Domain.Enums;

namespace IMS.Modular.Modules.Issues.Application.DTOs;

public record IssueDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid? AssigneeId,
    Guid ReporterId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DueDate,
    List<IssueCommentDto> Comments,
    List<IssueActivityDto> Activities,
    List<IssueTagDto> Tags);

public record IssueCommentDto(
    Guid Id,
    string Content,
    Guid AuthorId,
    DateTime CreatedAt);

public record IssueActivityDto(
    Guid Id,
    string ActivityType,
    string Description,
    Guid UserId,
    DateTime Timestamp);

public record IssueTagDto(
    string Name,
    string Color);

public record CreateIssueRequest(
    string Title,
    string Description,
    IssuePriority Priority,
    DateTime? DueDate);

public record UpdateIssueRequest(
    string? Title,
    string? Description,
    IssuePriority? Priority,
    DateTime? DueDate);

public record UpdateStatusRequest(
    string Status);

public record AssignIssueRequest(
    Guid AssigneeId);

public record AddCommentRequest(
    string Content);

public record AddTagRequest(
    string Name,
    string Color);
