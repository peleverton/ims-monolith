using IMS.Modular.Modules.Issues.Domain.Enums;

namespace IMS.Modular.Modules.SmartAssigner.Domain.Models;

/// <summary>
/// Context passed through the Chain of Responsibility for assignment decision.
/// </summary>
public sealed class AssignmentContext
{
    public Guid IssueId { get; init; }
    public string IssueTitle { get; init; } = string.Empty;
    public IssuePriority Priority { get; init; }
    public List<string> RequiredTags { get; init; } = [];

    /// <summary>
    /// All candidates still eligible after each handler filters.
    /// </summary>
    public List<CandidateScore> Candidates { get; set; } = [];

    /// <summary>
    /// The final selected assignee (set at the end of the chain).
    /// </summary>
    public Guid? SelectedAssigneeId { get; set; }

    /// <summary>
    /// Reason for the assignment decision (audit trail).
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
