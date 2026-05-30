using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.SmartAssigner.Application.Chain;

/// <summary>
/// Step 2: Filters candidates based on skills (tags).
/// If the issue has required tags, only candidates with matching skills pass.
/// If no candidates match skills, falls back to all available (soft filter).
/// </summary>
public sealed class CheckSkillsHandler(
    ILogger<CheckSkillsHandler> logger) : IAssignmentHandler
{
    public Task<bool> HandleAsync(AssignmentContext context, CancellationToken ct = default)
    {
        if (context.RequiredTags.Count == 0)
        {
            logger.LogInformation("[SmartAssigner] No skill requirements for Issue={IssueId}, skipping filter",
                context.IssueId);
            return Task.FromResult(true);
        }

        var skilled = context.Candidates
            .Where(c => context.RequiredTags.Any(tag =>
                c.Skills.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        if (skilled.Count > 0)
        {
            logger.LogInformation(
                "[SmartAssigner] Skills filter: {Before} → {After} candidates (tags: {Tags})",
                context.Candidates.Count, skilled.Count, string.Join(", ", context.RequiredTags));
            context.Candidates = skilled;
        }
        else
        {
            logger.LogInformation(
                "[SmartAssigner] No candidates match required skills, keeping all {Count} available",
                context.Candidates.Count);
        }

        return Task.FromResult(true);
    }
}
