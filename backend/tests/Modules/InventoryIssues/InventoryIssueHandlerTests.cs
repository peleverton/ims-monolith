using FluentAssertions;
using IMS.Modular.Modules.InventoryIssues.Application.Commands;
using IMS.Modular.Modules.InventoryIssues.Application.Handlers;
using IMS.Modular.Modules.InventoryIssues.Domain.Entities;
using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IMS.Modular.Tests.Modules.InventoryIssues;

/// <summary>
/// US-053: Unit tests for InventoryIssues command handlers using EF InMemory.
/// Pattern: AAA (Arrange / Act / Assert)
/// </summary>
public class InventoryIssueHandlerTests : IDisposable
{
    private readonly InventoryIssuesDbContext _db;
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly IInventoryIssueRepository _repo;

    public InventoryIssueHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryIssuesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new InventoryIssuesDbContext(options, _mediatorMock.Object);
        _repo = new InventoryIssueRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ────────────────────────────────────────────────────────────────
    // CreateInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInventoryIssue_ValidCommand_ReturnsPersistentGuid()
    {
        // Arrange
        var handler = new CreateInventoryIssueHandler(_repo);
        var reporterId = Guid.NewGuid();
        var cmd = new CreateInventoryIssueCommand(
            "Low stock detected", "Product A is below minimum threshold",
            InventoryIssueType.Shortage, InventoryIssuePriority.High,
            reporterId, Guid.NewGuid(), null, 5, 250m, null);

        // Act
        var id = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        id.Should().NotBeEmpty();
        var saved = await _db.InventoryIssues.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Low stock detected");
        saved.Status.Should().Be(InventoryIssueStatus.Open);
        saved.ReporterId.Should().Be(reporterId);
    }

    [Fact]
    public async Task CreateInventoryIssue_WithOptionalFields_PersistsCorrectly()
    {
        // Arrange
        var handler = new CreateInventoryIssueHandler(_repo);
        var productId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(7);
        var cmd = new CreateInventoryIssueCommand(
            "Expired batch", "Batch X expired",
            InventoryIssueType.Expiry, InventoryIssuePriority.Critical,
            Guid.NewGuid(), productId, null, 100, 1500m, dueDate);

        // Act
        var id = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        var saved = await _db.InventoryIssues.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.ProductId.Should().Be(productId);
        saved.AffectedQuantity.Should().Be(100);
        saved.EstimatedLoss.Should().Be(1500m);
        saved.DueDate.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
    }

    // ────────────────────────────────────────────────────────────────
    // UpdateInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateInventoryIssue_ExistingIssue_ReturnsTrue()
    {
        // Arrange
        var issue = CreateIssue("Original Title");
        await _repo.AddAsync(issue);

        var handler = new UpdateInventoryIssueHandler(_repo);
        var cmd = new UpdateInventoryIssueCommand(
            issue.Id, "Updated Title", "Updated description",
            InventoryIssueType.Shortage, InventoryIssuePriority.Medium,
            null, null, null, null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var updated = await _db.InventoryIssues.FindAsync(issue.Id);
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateInventoryIssue_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var handler = new UpdateInventoryIssueHandler(_repo);
        var cmd = new UpdateInventoryIssueCommand(
            Guid.NewGuid(), "Title", "Desc",
            InventoryIssueType.Shortage, InventoryIssuePriority.Low,
            null, null, null, null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────
    // AssignInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignInventoryIssue_ExistingIssue_SetsAssigneeAndReturnsTrue()
    {
        // Arrange
        var issue = CreateIssue("Assign me");
        await _repo.AddAsync(issue);

        var handler = new AssignInventoryIssueHandler(_repo);
        var assigneeId = Guid.NewGuid();
        var cmd = new AssignInventoryIssueCommand(issue.Id, assigneeId, Guid.NewGuid());

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var updated = await _db.InventoryIssues.FindAsync(issue.Id);
        updated!.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task AssignInventoryIssue_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var handler = new AssignInventoryIssueHandler(_repo);

        // Act
        var result = await handler.Handle(new AssignInventoryIssueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────
    // ResolveInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveInventoryIssue_OpenIssue_ChangesStatusToResolved()
    {
        // Arrange
        var issue = CreateIssue("Resolve me");
        await _repo.AddAsync(issue);

        var handler = new ResolveInventoryIssueHandler(_repo);
        var cmd = new ResolveInventoryIssueCommand(issue.Id, "Fixed by replenishing stock", Guid.NewGuid());

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var resolved = await _db.InventoryIssues.FindAsync(issue.Id);
        resolved!.Status.Should().Be(InventoryIssueStatus.Resolved);
        resolved.ResolutionNotes.Should().Be("Fixed by replenishing stock");
    }

    [Fact]
    public async Task ResolveInventoryIssue_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var handler = new ResolveInventoryIssueHandler(_repo);

        // Act
        var result = await handler.Handle(new ResolveInventoryIssueCommand(Guid.NewGuid(), null, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────
    // CloseInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseInventoryIssue_ResolvedIssue_ChangesStatusToClosed()
    {
        // Arrange
        var issue = CreateIssue("Close me");
        await _repo.AddAsync(issue);
        issue.Resolve("Resolution notes", Guid.NewGuid());
        await _repo.UpdateAsync(issue);

        var handler = new CloseInventoryIssueHandler(_repo);
        var cmd = new CloseInventoryIssueCommand(issue.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var closed = await _db.InventoryIssues.FindAsync(issue.Id);
        closed!.Status.Should().Be(InventoryIssueStatus.Closed);
    }

    // ────────────────────────────────────────────────────────────────
    // ReopenInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReopenInventoryIssue_ClosedIssue_ChangesStatusBackToOpen()
    {
        // Arrange
        var issue = CreateIssue("Reopen me");
        await _repo.AddAsync(issue);
        issue.Resolve("Resolved", Guid.NewGuid());
        issue.Close(Guid.NewGuid());
        await _repo.UpdateAsync(issue);

        var handler = new ReopenInventoryIssueHandler(_repo);
        var cmd = new ReopenInventoryIssueCommand(issue.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var reopened = await _db.InventoryIssues.FindAsync(issue.Id);
        // Reopen sets status to Reopened (not Open) — preserving history
        reopened!.Status.Should().Be(InventoryIssueStatus.Reopened);
    }

    // ────────────────────────────────────────────────────────────────
    // DeleteInventoryIssueHandler
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteInventoryIssue_ExistingIssue_RemovesFromDatabase()
    {
        // Arrange
        var issue = CreateIssue("Delete me");
        await _repo.AddAsync(issue);

        var handler = new DeleteInventoryIssueHandler(_repo);
        var cmd = new DeleteInventoryIssueCommand(issue.Id);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var deleted = await _db.InventoryIssues.FindAsync(issue.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteInventoryIssue_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var handler = new DeleteInventoryIssueHandler(_repo);

        // Act
        var result = await handler.Handle(new DeleteInventoryIssueCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────
    // Domain Entity — InventoryIssue lifecycle
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryIssue_NewInstance_HasOpenStatus()
    {
        // Act
        var issue = CreateIssue("Status check");

        // Assert
        issue.Status.Should().Be(InventoryIssueStatus.Open);
    }

    [Fact]
    public void InventoryIssue_Resolve_SetsStatusAndNotes()
    {
        // Arrange
        var issue = CreateIssue("To resolve");

        // Act
        issue.Resolve("Fixed it", Guid.NewGuid());

        // Assert
        issue.Status.Should().Be(InventoryIssueStatus.Resolved);
        issue.ResolutionNotes.Should().Be("Fixed it");
        issue.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void InventoryIssue_Reopen_SetsReopenedStatus()
    {
        // Arrange
        var issue = CreateIssue("To reopen");
        issue.Resolve("Resolved", Guid.NewGuid());
        issue.Close(Guid.NewGuid());

        // Act
        issue.Reopen(Guid.NewGuid());

        // Assert — domain uses Reopened status (not Open) to preserve history
        issue.Status.Should().Be(InventoryIssueStatus.Reopened);
    }

    [Fact]
    public void InventoryIssue_Create_EmptyTitle_ThrowsArgumentException()
    {
        // Act
        var act = () => new InventoryIssue(
            "", "Description",
            InventoryIssueType.Shortage, InventoryIssuePriority.Low,
            Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────

    private static InventoryIssue CreateIssue(string title) =>
        new(title, "Test description",
            InventoryIssueType.Shortage, InventoryIssuePriority.Medium,
            Guid.NewGuid());
}
