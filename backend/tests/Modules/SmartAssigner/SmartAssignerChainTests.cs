using FluentAssertions;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.SmartAssigner.Application.Chain;
using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace IMS.Modular.Tests.Modules.SmartAssigner;

public class SmartAssignerChainTests
{
    private static readonly Guid User1 = Guid.NewGuid();
    private static readonly Guid User2 = Guid.NewGuid();
    private static readonly Guid User3 = Guid.NewGuid();

    private static List<CandidateScore> CreateCandidates() =>
    [
        new() { UserId = User1, Username = "alice", ActiveIssues = 3, OverdueIssues = 0, AvgResolutionHours = 4.0, IsAvailable = true, Skills = { "backend", "database" } },
        new() { UserId = User2, Username = "bob", ActiveIssues = 7, OverdueIssues = 2, AvgResolutionHours = 8.0, IsAvailable = true, Skills = { "frontend", "css" } },
        new() { UserId = User3, Username = "carol", ActiveIssues = 1, OverdueIssues = 0, AvgResolutionHours = 2.5, IsAvailable = false, Skills = { "backend", "devops" } },
    ];

    // ─── CheckAvailabilityHandler ─────────────────────────────────────────────

    [Fact]
    public async Task CheckAvailability_FiltersOutUnavailableCandidates()
    {
        var handler = new CheckAvailabilityHandler(NullLogger<CheckAvailabilityHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            Candidates = CreateCandidates()
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeTrue();
        context.Candidates.Should().HaveCount(2);
        context.Candidates.Should().NotContain(c => c.UserId == User3);
    }

    [Fact]
    public async Task CheckAvailability_NoCandidates_StopsChain()
    {
        var handler = new CheckAvailabilityHandler(NullLogger<CheckAvailabilityHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            Candidates = [new() { UserId = User3, Username = "carol", IsAvailable = false }]
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeFalse();
        context.Reason.Should().Contain("No available candidates");
    }

    // ─── CheckSkillsHandler ───────────────────────────────────────────────────

    [Fact]
    public async Task CheckSkills_WithMatchingTags_FiltersToSkilledCandidates()
    {
        var handler = new CheckSkillsHandler(NullLogger<CheckSkillsHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            RequiredTags = ["backend"],
            Candidates = CreateCandidates().Where(c => c.IsAvailable).ToList()
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeTrue();
        context.Candidates.Should().HaveCount(1);
        context.Candidates[0].UserId.Should().Be(User1);
    }

    [Fact]
    public async Task CheckSkills_NoMatchingTags_KeepsAllCandidates()
    {
        var handler = new CheckSkillsHandler(NullLogger<CheckSkillsHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            RequiredTags = ["machine-learning"],
            Candidates = CreateCandidates().Where(c => c.IsAvailable).ToList()
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeTrue();
        context.Candidates.Should().HaveCount(2); // fallback: keeps all
    }

    [Fact]
    public async Task CheckSkills_NoRequiredTags_SkipsFilter()
    {
        var handler = new CheckSkillsHandler(NullLogger<CheckSkillsHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            RequiredTags = [],
            Candidates = CreateCandidates().Where(c => c.IsAvailable).ToList()
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeTrue();
        context.Candidates.Should().HaveCount(2);
    }

    // ─── CheckWorkloadCapacityHandler ─────────────────────────────────────────

    [Fact]
    public async Task CheckWorkload_SelectsLowestScoreCandidate()
    {
        var handler = new CheckWorkloadCapacityHandler(NullLogger<CheckWorkloadCapacityHandler>.Instance);
        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            Candidates = CreateCandidates().Where(c => c.IsAvailable).ToList()
        };

        var result = await handler.HandleAsync(context);

        result.Should().BeTrue();
        // Alice: 3*3 + 0*5 + 4.0*0.1 = 9.4
        // Bob:   7*3 + 2*5 + 8.0*0.1 = 31.8
        context.SelectedAssigneeId.Should().Be(User1);
        context.Reason.Should().Contain("alice");
    }

    // ─── Full Chain Integration ───────────────────────────────────────────────

    [Fact]
    public async Task FullChain_AssignsBestCandidateByWorkload()
    {
        var handlers = new IAssignmentHandler[]
        {
            new CheckAvailabilityHandler(NullLogger<CheckAvailabilityHandler>.Instance),
            new CheckSkillsHandler(NullLogger<CheckSkillsHandler>.Instance),
            new CheckWorkloadCapacityHandler(NullLogger<CheckWorkloadCapacityHandler>.Instance)
        };

        var executor = new AssignmentChainExecutor(
            handlers, NullLogger<AssignmentChainExecutor>.Instance);

        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            IssueTitle = "Fix production bug",
            Priority = IssuePriority.High,
            RequiredTags = ["backend"],
            Candidates = CreateCandidates()
        };

        var result = await executor.ExecuteAsync(context);

        // Carol is unavailable, Bob doesn't have "backend" skill
        // Alice is the only eligible candidate
        result.SelectedAssigneeId.Should().Be(User1);
        result.Reason.Should().Contain("alice");
    }

    [Fact]
    public async Task FullChain_NoRequiredSkills_SelectsLeastLoaded()
    {
        var handlers = new IAssignmentHandler[]
        {
            new CheckAvailabilityHandler(NullLogger<CheckAvailabilityHandler>.Instance),
            new CheckSkillsHandler(NullLogger<CheckSkillsHandler>.Instance),
            new CheckWorkloadCapacityHandler(NullLogger<CheckWorkloadCapacityHandler>.Instance)
        };

        var executor = new AssignmentChainExecutor(
            handlers, NullLogger<AssignmentChainExecutor>.Instance);

        var context = new AssignmentContext
        {
            IssueId = Guid.NewGuid(),
            IssueTitle = "General task",
            Priority = IssuePriority.Low,
            RequiredTags = [],
            Candidates = CreateCandidates()
        };

        var result = await executor.ExecuteAsync(context);

        // Carol is unavailable, between Alice and Bob — Alice has lower score
        result.SelectedAssigneeId.Should().Be(User1);
    }

    // ─── CandidateScore ───────────────────────────────────────────────────────

    [Fact]
    public void CandidateScore_CalculatesWeightedScore()
    {
        var candidate = new CandidateScore
        {
            ActiveIssues = 5,
            OverdueIssues = 2,
            AvgResolutionHours = 10.0
        };

        // 5*3 + 2*5 + 10*0.1 = 15 + 10 + 1 = 26
        candidate.Score.Should().Be(26.0);
    }

    [Fact]
    public void CandidateScore_CompareTo_LowerScoreFirst()
    {
        var low = new CandidateScore { ActiveIssues = 1, OverdueIssues = 0, AvgResolutionHours = 1.0 };
        var high = new CandidateScore { ActiveIssues = 10, OverdueIssues = 3, AvgResolutionHours = 20.0 };

        low.CompareTo(high).Should().BeLessThan(0);
    }
}
