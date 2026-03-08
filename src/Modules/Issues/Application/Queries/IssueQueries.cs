using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Common;
using MediatR;

namespace IMS.Modular.Modules.Issues.Application.Queries;

public record GetIssueByIdQuery(Guid Id) : IRequest<IssueDto?>;

public record GetAllIssuesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SortBy = null,
    string SortDirection = "asc",
    IssueStatus? Status = null,
    IssuePriority? Priority = null,
    string? SearchTerm = null) : IRequest<PagedResult<IssueDto>>, ICacheable
{
    public string CacheKeyPrefix => "issues-list";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record GetIssuesByStatusQuery(
    IssueStatus Status,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<IssueDto>>;

public record GetIssuesByPriorityQuery(
    IssuePriority Priority,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<IssueDto>>;

public record SearchIssuesQuery(
    string SearchTerm,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<IssueDto>>;

public record GetUserIssuesQuery(
    Guid UserId,
    bool AsAssignee = true,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<IssueDto>>;
