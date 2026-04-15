using FluentAssertions;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Domain.Events;

namespace IMS.Modular.Tests.Modules.Issues;

/// <summary>
/// US-026: Unit tests for the Issue domain entity.
/// Pattern: AAA (Arrange / Act / Assert)
/// </summary>
public class IssueEntityTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // ----------------------------------------------------------------
    // Construction
    // ----------------------------------------------------------------

    [Fact]
    public void Constructor_ValidArgs_CreatesIssueWithOpenStatus()
    {
        // Arrange & Act
        var issue = new Issue("Fix login bug", "Users cannot login", IssuePriority.High, UserId);

        // Assert
        issue.Title.Should().Be("Fix login bug");
        issue.Description.Should().Be("Users cannot login");
        issue.Status.Should().Be(IssueStatus.Open);
        issue.Priority.Should().Be(IssuePriority.High);
        issue.ReporterId.Should().Be(UserId);
        issue.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_EmptyTitle_ThrowsArgumentException()
    {
        // Arrange & Act
        Action act = () => new Issue("", "Description", IssuePriority.Low, UserId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ValidArgs_RaisesIssueCreatedDomainEvent()
    {
        // Arrange & Act
        var issue = new Issue("New issue", "Description", IssuePriority.Medium, UserId);

        // Assert
        issue.DomainEvents.Should().ContainSingle(e => e is IssueCreatedEvent);
        var evt = (IssueCreatedEvent)issue.DomainEvents.First();
        evt.IssueId.Should().Be(issue.Id);
        evt.Title.Should().Be("New issue");
        evt.ReporterId.Should().Be(UserId);
    }

    // ----------------------------------------------------------------
    // UpdateTitle
    // ----------------------------------------------------------------

    [Fact]
    public void UpdateTitle_ValidTitle_ChangesTitleAndAddsActivity()
    {
        // Arrange
        var issue = new Issue("Old Title", "Desc", IssuePriority.Low, UserId);

        // Act
        issue.UpdateTitle("New Title", UserId);

        // Assert
        issue.Title.Should().Be("New Title");
        issue.Activities.Should().Contain(a => a.Description.Contains("New Title"));
    }

    [Fact]
    public void UpdateTitle_EmptyTitle_ThrowsArgumentException()
    {
        var issue = new Issue("Title", "Desc", IssuePriority.Low, UserId);
        Action act = () => issue.UpdateTitle("   ", UserId);
        act.Should().Throw<ArgumentException>();
    }

    // ----------------------------------------------------------------
    // UpdateStatus
    // ----------------------------------------------------------------

    [Fact]
    public void UpdateStatus_ToInProgress_ChangesStatusAndRaisesEvent()
    {
        // Arrange
        var issue = new Issue("Title", "Desc", IssuePriority.High, UserId);
        issue.ClearDomainEvents();

        // Act
        issue.UpdateStatus(IssueStatus.InProgress, UserId);

        // Assert
        issue.Status.Should().Be(IssueStatus.InProgress);
        issue.DomainEvents.Should().ContainSingle(e => e is IssueStatusChangedEvent);
        var evt = (IssueStatusChangedEvent)issue.DomainEvents.First();
        evt.OldStatus.Should().Be(IssueStatus.Open);
        evt.NewStatus.Should().Be(IssueStatus.InProgress);
    }

    [Theory]
    [InlineData(IssueStatus.Closed)]
    [InlineData(IssueStatus.Resolved)]
    public void UpdateStatus_ToClosedOrResolved_SetsClosedAt(IssueStatus closingStatus)
    {
        // Arrange
        var issue = new Issue("Title", "Desc", IssuePriority.Low, UserId);

        // Act
        issue.UpdateStatus(closingStatus, UserId);

        // Assert
        issue.Status.Should().Be(closingStatus);
    }

    // ----------------------------------------------------------------
    // Assign
    // ----------------------------------------------------------------

    [Fact]
    public void Assign_ValidAssignee_SetsAssigneeAndRaisesEvent()
    {
        // Arrange
        var issue = new Issue("Title", "Desc", IssuePriority.Medium, UserId);
        var assigneeId = Guid.NewGuid();
        issue.ClearDomainEvents();

        // Act
        issue.AssignTo(assigneeId, UserId);

        // Assert
        issue.AssigneeId.Should().Be(assigneeId);
        issue.DomainEvents.Should().ContainSingle(e => e is IssueAssignedEvent);
    }

    // ----------------------------------------------------------------
    // AddComment
    // ----------------------------------------------------------------

    [Fact]
    public void AddComment_ValidContent_AddsToCommentsList()
    {
        // Arrange
        var issue = new Issue("Title", "Desc", IssuePriority.Low, UserId);

        // Act
        issue.AddComment("This is a comment", UserId);

        // Assert
        issue.Comments.Should().HaveCount(1);
        issue.Comments[0].Content.Should().Be("This is a comment");
        issue.Comments[0].AuthorId.Should().Be(UserId);
    }

    // ----------------------------------------------------------------
    // Domain Events
    // ----------------------------------------------------------------

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var issue = new Issue("Title", "Desc", IssuePriority.High, UserId);
        issue.DomainEvents.Should().NotBeEmpty();

        // Act
        issue.ClearDomainEvents();

        // Assert
        issue.DomainEvents.Should().BeEmpty();
    }
}
