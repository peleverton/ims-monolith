using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.SmartAssigner.Application.Chain;

/// <summary>
/// Step 3: Selects the candidate with the lowest workload score (Min-Heap logic).
/// Uses a Priority Queue to efficiently find the best candidate.
/// Critical issues bump overloaded candidates down further.
/// </summary>
public sealed class CheckWorkloadCapacityHandler(
    ILogger<CheckWorkloadCapacityHandler> logger) : IAssignmentHandler
{
    private const int MaxActiveIssuesPerUser = 15;

    public Task<bool> HandleAsync(AssignmentContext context, CancellationToken ct = default)
    {
        // Filter out overloaded users (capacity check)
        var eligible = context.Candidates
            .Where(c => c.ActiveIssues < MaxActiveIssuesPerUser)
            .ToList();

        if (eligible.Count == 0)
        {
            // If all are overloaded, still pick the least loaded
            logger.LogWarning(
                "[SmartAssigner] All candidates at capacity for Issue={IssueId}, selecting least loaded",
                context.IssueId);
            eligible = context.Candidates;
        }

        // Min-Heap: PriorityQueue gives us the candidate with lowest score first
        var heap = new PriorityQueue<CandidateScore, double>();
        foreach (var candidate in eligible)
        {
            heap.Enqueue(candidate, candidate.Score);
        }

        if (heap.TryDequeue(out var best, out var score))
        {
            context.SelectedAssigneeId = best.UserId;
            context.Reason = $"Selected {best.Username} (score={score:F1}, active={best.ActiveIssues}, " +
                             $"overdue={best.OverdueIssues}, avgResolution={best.AvgResolutionHours:F1}h)";

            logger.LogInformation(
                "[SmartAssigner] Best candidate for Issue={IssueId}: User={UserId} Score={Score:F1}",
                context.IssueId, best.UserId, score);
        }
        else
        {
            context.Reason = "No eligible candidates after workload check";
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
