using IMS.Modular.Modules.InventoryIssues.Application.Commands;
using IMS.Modular.Modules.InventoryIssues.Application.Queries;
using IMS.Modular.Modules.InventoryIssues.Domain.Entities;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.InventoryIssues.Application.Handlers;

// ── Command Handlers ─────────────────────────────────────────────────────

public class CreateInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<CreateInventoryIssueCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = new InventoryIssue(
            cmd.Title, cmd.Description, cmd.Type, cmd.Priority, cmd.ReporterId,
            cmd.ProductId, cmd.LocationId, cmd.AffectedQuantity, cmd.EstimatedLoss, cmd.DueDate);
        await repo.AddAsync(issue, ct);
        return issue.Id;
    }
}

public class UpdateInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<UpdateInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(UpdateInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        issue.Update(cmd.Title, cmd.Description, cmd.Type, cmd.Priority,
            cmd.ProductId, cmd.LocationId, cmd.AffectedQuantity, cmd.EstimatedLoss, cmd.DueDate);
        await repo.UpdateAsync(issue, ct);
        return true;
    }
}

public class AssignInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<AssignInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(AssignInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        issue.Assign(cmd.AssigneeId, cmd.UserId);
        await repo.UpdateAsync(issue, ct);
        return true;
    }
}

public class ResolveInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<ResolveInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(ResolveInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        issue.Resolve(cmd.ResolutionNotes, cmd.UserId);
        await repo.UpdateAsync(issue, ct);
        return true;
    }
}

public class CloseInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<CloseInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(CloseInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        issue.Close(cmd.UserId);
        await repo.UpdateAsync(issue, ct);
        return true;
    }
}

public class ReopenInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<ReopenInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(ReopenInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        issue.Reopen(cmd.UserId);
        await repo.UpdateAsync(issue, ct);
        return true;
    }
}

public class DeleteInventoryIssueHandler(IInventoryIssueRepository repo)
    : IRequestHandler<DeleteInventoryIssueCommand, bool>
{
    public async Task<bool> Handle(DeleteInventoryIssueCommand cmd, CancellationToken ct)
    {
        var issue = await repo.GetByIdAsync(cmd.Id, ct);
        if (issue is null) return false;
        await repo.DeleteAsync(issue, ct);
        return true;
    }
}

// ── Query Handlers ───────────────────────────────────────────────────────

public class GetInventoryIssueByIdHandler(IInventoryIssueReadRepository read)
    : IRequestHandler<GetInventoryIssueByIdQuery, InventoryIssueReadDto?>
{
    public Task<InventoryIssueReadDto?> Handle(GetInventoryIssueByIdQuery q, CancellationToken ct)
        => read.GetByIdAsync(q.Id, ct);
}

public class GetInventoryIssuesHandler(IInventoryIssueReadRepository read)
    : IRequestHandler<GetInventoryIssuesQuery, PagedResult<InventoryIssueSummaryDto>>
{
    public Task<PagedResult<InventoryIssueSummaryDto>> Handle(GetInventoryIssuesQuery q, CancellationToken ct)
        => read.GetPagedAsync(q.Page, q.PageSize, q.Status, q.Priority, q.Type,
            q.ProductId, q.LocationId, q.ReporterId, q.AssigneeId, q.Search, ct);
}

public class GetOverdueInventoryIssuesHandler(IInventoryIssueReadRepository read)
    : IRequestHandler<GetOverdueInventoryIssuesQuery, PagedResult<InventoryIssueSummaryDto>>
{
    public Task<PagedResult<InventoryIssueSummaryDto>> Handle(GetOverdueInventoryIssuesQuery q, CancellationToken ct)
        => read.GetOverdueAsync(q.Page, q.PageSize, ct);
}

public class GetHighPriorityInventoryIssuesHandler(IInventoryIssueReadRepository read)
    : IRequestHandler<GetHighPriorityInventoryIssuesQuery, PagedResult<InventoryIssueSummaryDto>>
{
    public Task<PagedResult<InventoryIssueSummaryDto>> Handle(GetHighPriorityInventoryIssuesQuery q, CancellationToken ct)
        => read.GetHighPriorityAsync(q.Page, q.PageSize, ct);
}

public class GetInventoryIssueStatisticsHandler(IInventoryIssueReadRepository read)
    : IRequestHandler<GetInventoryIssueStatisticsQuery, InventoryIssueStatsDto>
{
    public Task<InventoryIssueStatsDto> Handle(GetInventoryIssueStatisticsQuery q, CancellationToken ct)
        => read.GetStatisticsAsync(ct);
}
