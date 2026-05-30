using IMS.Modular.Modules.SmartAssigner.Domain.Models;

namespace IMS.Modular.Modules.SmartAssigner.Infrastructure;

/// <summary>
/// Reads candidate data (workload, skills) from the database for assignment decisions.
/// </summary>
public interface ISmartAssignerRepository
{
    /// <summary>
    /// Gets all active users with their workload metrics and skills.
    /// </summary>
    Task<List<CandidateScore>> GetCandidatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the tags associated with an issue (used to match skills).
    /// </summary>
    Task<List<string>> GetIssueTagsAsync(Guid issueId, CancellationToken ct = default);
}
