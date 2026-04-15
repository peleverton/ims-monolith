using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Application.Mappings;
using IMS.Modular.Modules.Issues.Application.Queries;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Issues.Application.Handlers;

public sealed class GetIssueByIdQueryHandler(IssuesDbContext db)
    : IRequestHandler<GetIssueByIdQuery, IssueDto?>
{
    public async Task<IssueDto?> Handle(GetIssueByIdQuery request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        return issue is null ? null : IssueMapper.ToDto(issue);
    }
}

public sealed class GetAllIssuesQueryHandler(IssuesDbContext db)
    : IRequestHandler<GetAllIssuesQuery, PagedResult<IssueDto>>
{
    public async Task<PagedResult<IssueDto>> Handle(GetAllIssuesQuery request, CancellationToken ct)
    {
        var query = db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.Priority.HasValue)
            query = query.Where(i => i.Priority == request.Priority.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(term) ||
                i.Description.ToLower().Contains(term));
        }

        query = query.ApplySorting(request.SortBy ?? "CreatedAt", request.SortDirection);

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new PagedResult<IssueDto>(
            paged.Items.Select(IssueMapper.ToDto).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}

public sealed class GetIssuesByStatusQueryHandler(IssuesDbContext db)
    : IRequestHandler<GetIssuesByStatusQuery, PagedResult<IssueDto>>
{
    public async Task<PagedResult<IssueDto>> Handle(GetIssuesByStatusQuery request, CancellationToken ct)
    {
        var query = db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .Where(i => i.Status == request.Status);

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new PagedResult<IssueDto>(
            paged.Items.Select(IssueMapper.ToDto).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}

public sealed class GetIssuesByPriorityQueryHandler(IssuesDbContext db)
    : IRequestHandler<GetIssuesByPriorityQuery, PagedResult<IssueDto>>
{
    public async Task<PagedResult<IssueDto>> Handle(GetIssuesByPriorityQuery request, CancellationToken ct)
    {
        var query = db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .Where(i => i.Priority == request.Priority);

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new PagedResult<IssueDto>(
            paged.Items.Select(IssueMapper.ToDto).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}

public sealed class SearchIssuesQueryHandler(IssuesDbContext db)
    : IRequestHandler<SearchIssuesQuery, PagedResult<IssueDto>>
{
    public async Task<PagedResult<IssueDto>> Handle(SearchIssuesQuery request, CancellationToken ct)
    {
        var term = request.SearchTerm.ToLower();
        var query = db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .Where(i =>
                i.Title.ToLower().Contains(term) ||
                i.Description.ToLower().Contains(term));

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new PagedResult<IssueDto>(
            paged.Items.Select(IssueMapper.ToDto).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}

public sealed class GetUserIssuesQueryHandler(IssuesDbContext db)
    : IRequestHandler<GetUserIssuesQuery, PagedResult<IssueDto>>
{
    public async Task<PagedResult<IssueDto>> Handle(GetUserIssuesQuery request, CancellationToken ct)
    {
        var query = db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .AsNoTracking()
            .Where(i => request.AsAssignee
                ? i.AssigneeId == request.UserId
                : i.ReporterId == request.UserId);

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new PagedResult<IssueDto>(
            paged.Items.Select(IssueMapper.ToDto).ToList(),
            paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}
