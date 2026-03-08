using IMS.Modular.Modules.Issues.Application.Commands;
using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Application.Mappings;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Issues.Application.Handlers;

public sealed class CreateIssueCommandHandler(
    IssuesDbContext db,
    ILogger<CreateIssueCommandHandler> logger)
    : IRequestHandler<CreateIssueCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(CreateIssueCommand request, CancellationToken ct)
    {
        logger.LogInformation("Creating issue: Title='{Title}', Reporter={ReporterId}", request.Title, request.ReporterId);

        var issue = new Issue(request.Title, request.Description, request.Priority, request.ReporterId, request.DueDate);
        db.Issues.Add(issue);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issue created: {IssueId}", issue.Id);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}

public sealed class UpdateIssueCommandHandler(
    IssuesDbContext db,
    ILogger<UpdateIssueCommandHandler> logger)
    : IRequestHandler<UpdateIssueCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(UpdateIssueCommand request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (issue is null)
            return Result<IssueDto>.NotFound($"Issue {request.Id} not found");

        if (!string.IsNullOrWhiteSpace(request.Title))
            issue.UpdateTitle(request.Title, request.UserId);
        if (request.Description is not null)
            issue.UpdateDescription(request.Description, request.UserId);
        if (request.Priority.HasValue)
            issue.UpdatePriority(request.Priority.Value, request.UserId);
        if (request.DueDate.HasValue)
            issue.UpdateDueDate(request.DueDate, request.UserId);

        issue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issue {IssueId} updated", request.Id);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}

public sealed class ChangeIssueStatusCommandHandler(
    IssuesDbContext db,
    ILogger<ChangeIssueStatusCommandHandler> logger)
    : IRequestHandler<ChangeIssueStatusCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(ChangeIssueStatusCommand request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (issue is null)
            return Result<IssueDto>.NotFound($"Issue {request.Id} not found");

        issue.UpdateStatus(request.Status, request.UserId);
        issue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issue {IssueId} status changed to {Status}", request.Id, request.Status);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}

public sealed class AssignIssueCommandHandler(
    IssuesDbContext db,
    ILogger<AssignIssueCommandHandler> logger)
    : IRequestHandler<AssignIssueCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(AssignIssueCommand request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (issue is null)
            return Result<IssueDto>.NotFound($"Issue {request.Id} not found");

        issue.AssignTo(request.AssigneeId, request.UserId);
        issue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issue {IssueId} assigned to {AssigneeId}", request.Id, request.AssigneeId);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}

public sealed class DeleteIssueCommandHandler(
    IssuesDbContext db,
    ILogger<DeleteIssueCommandHandler> logger)
    : IRequestHandler<DeleteIssueCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteIssueCommand request, CancellationToken ct)
    {
        var issue = await db.Issues.FindAsync([request.Id], ct);
        if (issue is null)
            return Result<bool>.NotFound($"Issue {request.Id} not found");

        db.Issues.Remove(issue);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issue {IssueId} deleted", request.Id);
        return Result<bool>.Success(true);
    }
}

public sealed class AddCommentCommandHandler(
    IssuesDbContext db,
    ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == request.IssueId, ct);

        if (issue is null)
            return Result<IssueDto>.NotFound($"Issue {request.IssueId} not found");

        issue.AddComment(request.Content, request.UserId);
        issue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Comment added to issue {IssueId}", request.IssueId);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}

public sealed class AddTagCommandHandler(
    IssuesDbContext db,
    ILogger<AddTagCommandHandler> logger)
    : IRequestHandler<AddTagCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(AddTagCommand request, CancellationToken ct)
    {
        var issue = await db.Issues
            .Include(i => i.Comments).Include(i => i.Activities).Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == request.IssueId, ct);

        if (issue is null)
            return Result<IssueDto>.NotFound($"Issue {request.IssueId} not found");

        issue.AddTag(request.Name, request.Color, request.UserId);
        issue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Tag '{Tag}' added to issue {IssueId}", request.Name, request.IssueId);
        return Result<IssueDto>.Success(IssueMapper.ToDto(issue));
    }
}
