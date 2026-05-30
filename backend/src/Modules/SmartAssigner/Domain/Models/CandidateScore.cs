namespace IMS.Modular.Modules.SmartAssigner.Domain.Models;

/// <summary>
/// Represents a candidate for issue assignment with their computed workload score.
/// Lower score = better candidate (less loaded, faster resolver).
/// </summary>
public sealed class CandidateScore : IComparable<CandidateScore>
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int ActiveIssues { get; init; }
    public int OverdueIssues { get; init; }
    public double AvgResolutionHours { get; init; }
    public List<string> Skills { get; init; } = [];
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Weighted score: lower is better.
    /// Formula: ActiveIssues * 3 + OverdueIssues * 5 + AvgResolutionHours * 0.1
    /// </summary>
    public double Score => (ActiveIssues * 3.0) + (OverdueIssues * 5.0) + (AvgResolutionHours * 0.1);

    public int CompareTo(CandidateScore? other)
    {
        if (other is null) return -1;
        return Score.CompareTo(other.Score);
    }
}
