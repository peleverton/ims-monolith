using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.SmartAssigner.Application.Chain;

/// <summary>
/// Step 1: Filters out candidates who are not currently available (inactive users).
/// </summary>
public sealed class CheckAvailabilityHandler(
    ILogger<CheckAvailabilityHandler> logger) : IAssignmentHandler
{
    public Task<bool> HandleAsync(AssignmentContext context, CancellationToken ct = default)
    {
        var before = context.Candidates.Count;
        context.Candidates = context.Candidates
            .Where(c => c.IsAvailable)
            .ToList();

        logger.LogInformation(
            "[SmartAssigner] Availability filter: {Before} → {After} candidates for Issue={IssueId}",
            before, context.Candidates.Count, context.IssueId);

        if (context.Candidates.Count == 0)
        {
            context.Reason = "No available candidates found";
            return Task.FromResult(false); // stop chain
        }

        return Task.FromResult(true); // continue
    }
}
