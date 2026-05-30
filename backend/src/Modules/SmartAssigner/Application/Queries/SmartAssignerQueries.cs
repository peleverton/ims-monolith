using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using MediatR;

namespace IMS.Modular.Modules.SmartAssigner.Application.Queries;

/// <summary>
/// Manually triggers the Smart Assigner for a specific issue.
/// Returns the assignment decision without persisting (dry-run mode when DryRun=true).
/// </summary>
public record TriggerSmartAssignCommand(
    Guid IssueId,
    bool DryRun = false) : IRequest<SmartAssignResultDto>;

public sealed class SmartAssignResultDto
{
    public Guid IssueId { get; init; }
    public Guid? AssignedTo { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool WasDryRun { get; init; }
    public List<CandidateScoreDto> Candidates { get; init; } = [];
}

public sealed class CandidateScoreDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int ActiveIssues { get; init; }
    public int OverdueIssues { get; init; }
    public double AvgResolutionHours { get; init; }
    public double Score { get; init; }
    public List<string> Skills { get; init; } = [];
}
