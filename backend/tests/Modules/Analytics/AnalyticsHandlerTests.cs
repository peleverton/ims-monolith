using FluentAssertions;
using IMS.Modular.Modules.Analytics.Application.DTOs;
using IMS.Modular.Modules.Analytics.Application.Handlers;
using IMS.Modular.Modules.Analytics.Application.Queries;
using IMS.Modular.Modules.Analytics.Infrastructure;
using Moq;

namespace IMS.Modular.Tests.Modules.Analytics;

/// <summary>
/// US-051: Unit tests for Analytics query handlers.
/// Pattern: AAA (Arrange / Act / Assert)
/// All database interactions are mocked via IAnalyticsReadRepository.
/// </summary>
public class AnalyticsHandlerTests
{
    private readonly Mock<IAnalyticsReadRepository> _repoMock = new();

    // ────────────────────────────────────────────────────────────────
    // GetIssueSummaryHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueSummary_ReturnsDataFromRepository()
    {
        // Arrange
        var expected = new IssueSummaryDto(10, 3, 2, 1, 3, 1, 0, 0);
        _repoMock.Setup(r => r.GetIssueSummaryAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetIssueSummaryHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetIssueSummaryQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(10);
        result.Open.Should().Be(3);
        result.Resolved.Should().Be(3);
        _repoMock.Verify(r => r.GetIssueSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetIssueSummary_EmptyDatabase_ReturnsZeroes()
    {
        // Arrange
        var expected = new IssueSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);
        _repoMock.Setup(r => r.GetIssueSummaryAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetIssueSummaryHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetIssueSummaryQuery(), CancellationToken.None);

        // Assert
        result.Total.Should().Be(0);
        result.Open.Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────
    // GetIssueStatsByStatusHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueStatsByStatus_ReturnsStatsList()
    {
        // Arrange
        IReadOnlyList<IssueStatsByStatusDto> expected = new List<IssueStatsByStatusDto>
        {
            new("Open", 5, 50.0),
            new("Closed", 5, 50.0)
        };
        _repoMock.Setup(r => r.GetIssueStatsByStatusAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetIssueStatsByStatusHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetIssueStatsByStatusQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Status.Should().Be("Open");
        result[0].Percentage.Should().Be(50.0);
    }

    // ────────────────────────────────────────────────────────────────
    // GetIssueStatsByPriorityHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueStatsByPriority_ReturnsStatsList()
    {
        // Arrange
        IReadOnlyList<IssueStatsByPriorityDto> expected = new List<IssueStatsByPriorityDto>
        {
            new("High", 4, 40.0),
            new("Medium", 6, 60.0)
        };
        _repoMock.Setup(r => r.GetIssueStatsByPriorityAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetIssueStatsByPriorityHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetIssueStatsByPriorityQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Priority.Should().Be("High");
        result[1].Count.Should().Be(6);
    }

    // ────────────────────────────────────────────────────────────────
    // GetDashboardHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_ComposesAllDataSources()
    {
        // Arrange
        var issueSummary = new IssueSummaryDto(5, 2, 1, 0, 1, 1, 0, 0);
        var inventorySummary = new InventorySummaryDto(100, 90, 5, 5, 0, 0, 1000m);
        IReadOnlyList<IssueTrendDto> trends = new List<IssueTrendDto>
        {
            new("2025-01-01", 3, 1, 0)
        };
        IReadOnlyList<UserWorkloadSummaryDto> workload = new List<UserWorkloadSummaryDto>
        {
            new(Guid.NewGuid(), 10, 3, 2, 4, 1, 0)
        };

        _repoMock.Setup(r => r.GetIssueSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(issueSummary);
        _repoMock.Setup(r => r.GetInventorySummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inventorySummary);
        _repoMock.Setup(r => r.GetIssueTrendsAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(trends);
        _repoMock.Setup(r => r.GetAllUsersWorkloadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(workload);

        var handler = new GetDashboardHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IssueSummary.Total.Should().Be(5);
        result.InventorySummary.TotalProducts.Should().Be(100);
        result.RecentTrends.Should().HaveCount(1);
        result.TopAssignees.Should().HaveCount(1);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ────────────────────────────────────────────────────────────────
    // GetIssueTrendsHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueTrends_PassesDaysParameterToRepository()
    {
        // Arrange
        IReadOnlyList<IssueTrendDto> expected = new List<IssueTrendDto>
        {
            new("2025-01-01", 2, 1, 0),
            new("2025-01-02", 1, 2, 1)
        };
        _repoMock.Setup(r => r.GetIssueTrendsAsync(30, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetIssueTrendsHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetIssueTrendsQuery(30), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetIssueTrendsAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ────────────────────────────────────────────────────────────────
    // GetAllUsersWorkloadHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersWorkload_ReturnsWorkloadList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        IReadOnlyList<UserWorkloadSummaryDto> expected = new List<UserWorkloadSummaryDto>
        {
            new(userId, 8, 3, 2, 2, 1, 1)
        };
        _repoMock.Setup(r => r.GetAllUsersWorkloadAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var handler = new GetAllUsersWorkloadHandler(_repoMock.Object);

        // Act
        var result = await handler.Handle(new GetAllUsersWorkloadQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
        result[0].TotalAssigned.Should().Be(8);
    }
}
