using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.SmartAssigner.Application.Chain;

/// <summary>
/// Orchestrates the Chain of Responsibility by executing handlers in sequence.
/// Stops early if any handler returns false.
/// </summary>
public sealed class AssignmentChainExecutor(
    IEnumerable<IAssignmentHandler> handlers,
    ILogger<AssignmentChainExecutor> logger)
{
    public async Task<AssignmentContext> ExecuteAsync(AssignmentContext context, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[SmartAssigner] Starting chain for Issue={IssueId} with {CandidateCount} candidates",
            context.IssueId, context.Candidates.Count);

        foreach (var handler in handlers)
        {
            var shouldContinue = await handler.HandleAsync(context, ct);

            if (!shouldContinue)
            {
                logger.LogInformation(
                    "[SmartAssigner] Chain stopped at {Handler} for Issue={IssueId}. Reason: {Reason}",
                    handler.GetType().Name, context.IssueId, context.Reason);
                break;
            }
        }

        logger.LogInformation(
            "[SmartAssigner] Chain result for Issue={IssueId}: SelectedAssignee={AssigneeId}, Reason={Reason}",
            context.IssueId, context.SelectedAssigneeId, context.Reason);

        return context;
    }
}
