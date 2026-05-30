using IMS.Modular.Modules.Issues.Application.Commands;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.SmartAssigner.Application.Chain;
using IMS.Modular.Modules.SmartAssigner.Application.Queries;
using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using IMS.Modular.Modules.SmartAssigner.Infrastructure;
using IMS.Modular.Shared.Messaging;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.SmartAssigner.Application.Handlers;

public sealed class TriggerSmartAssignHandler(
    ISmartAssignerRepository repository,
    AssignmentChainExecutor chainExecutor,
    IMediator mediator,
    IHubContext<NotificationsHub> hub,
    ILogger<TriggerSmartAssignHandler> logger) : IRequestHandler<TriggerSmartAssignCommand, SmartAssignResultDto>
{
    public async Task<SmartAssignResultDto> Handle(TriggerSmartAssignCommand request, CancellationToken ct)
    {
        var candidates = await repository.GetCandidatesAsync(ct);
        var issueTags = await repository.GetIssueTagsAsync(request.IssueId, ct);

        var context = new AssignmentContext
        {
            IssueId = request.IssueId,
            IssueTitle = string.Empty,
            Priority = IssuePriority.Medium,
            RequiredTags = issueTags,
            Candidates = candidates
        };

        var result = await chainExecutor.ExecuteAsync(context, ct);

        // If not dry-run and we have a candidate, actually assign
        if (!request.DryRun && result.SelectedAssigneeId is not null)
        {
            var assignResult = await mediator.Send(
                new AssignIssueCommand(request.IssueId, result.SelectedAssigneeId.Value, Guid.Empty), ct);

            if (assignResult.IsSuccess)
            {
                logger.LogInformation(
                    "[SmartAssigner] Manual trigger: Issue={IssueId} assigned to User={UserId}",
                    request.IssueId, result.SelectedAssigneeId);

                await hub.Clients.All.SendAsync("IssueAutoAssigned", new
                {
                    issueId = request.IssueId,
                    assigneeId = result.SelectedAssigneeId,
                    reason = result.Reason,
                    occurredOn = DateTime.UtcNow
                }, ct);
            }
        }

        return new SmartAssignResultDto
        {
            IssueId = request.IssueId,
            AssignedTo = result.SelectedAssigneeId,
            Reason = result.Reason,
            WasDryRun = request.DryRun,
            Candidates = candidates.Select(c => new CandidateScoreDto
            {
                UserId = c.UserId,
                Username = c.Username,
                ActiveIssues = c.ActiveIssues,
                OverdueIssues = c.OverdueIssues,
                AvgResolutionHours = c.AvgResolutionHours,
                Score = c.Score,
                Skills = c.Skills
            }).ToList()
        };
    }
}
