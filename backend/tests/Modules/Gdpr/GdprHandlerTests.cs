using FluentAssertions;
using Hangfire;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Jobs;
using IMS.Modular.Modules.Notifications.Domain.Entities;
using IMS.Modular.Modules.Notifications.Infrastructure;
using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.Handlers;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IMS.Modular.Tests.Modules.Gdpr;

/// <summary>
/// US-089: LGPD/GDPR unit tests.
/// Covers data export, delete request creation/cancellation and hard-delete job.
/// </summary>
public class GdprHandlerTests : IDisposable
{
    private readonly AuthDbContext _auth;
    private readonly IssuesDbContext _issues;
    private readonly NotificationsDbContext _notifs;
    private readonly GdprRepository _repo;

    private static readonly Guid AdminId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid UserId  = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserRoleId  = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public GdprHandlerTests()
    {
        var dbName = Guid.NewGuid().ToString();

        _auth = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>().UseInMemoryDatabase(dbName).Options);

        var tenantMock = new Mock<ITenantService>();
        tenantMock.Setup(t => t.TenantId).Returns("test");
        var mediatorMock = new Mock<IMediator>();

        _issues = new IssuesDbContext(
            new DbContextOptionsBuilder<IssuesDbContext>().UseInMemoryDatabase(dbName).Options,
            mediatorMock.Object,
            tenantMock.Object);

        _notifs = new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>().UseInMemoryDatabase(dbName).Options);

        _repo = new GdprRepository(_auth, _issues, _notifs);

        SeedData();
    }

    private void SeedData()
    {
        var adminRole = new Role { Id = AdminRoleId, Name = "Admin" };
        var userRole  = new Role { Id = UserRoleId,  Name = "User"  };
        _auth.Roles.AddRange(adminRole, userRole);

        var admin = new User
        {
            Id = AdminId, Username = "admin", Email = "admin@test.com",
            FullName = "Admin", PasswordHash = "h", IsActive = true
        };
        admin.UserRoles.Add(new UserRole { UserId = AdminId, RoleId = AdminRoleId, Role = adminRole });

        var user = new User
        {
            Id = UserId, Username = "alice", Email = "alice@test.com",
            FullName = "Alice", PasswordHash = "h", IsActive = true
        };
        user.UserRoles.Add(new UserRole { UserId = UserId, RoleId = UserRoleId, Role = userRole });

        _auth.Users.AddRange(admin, user);
        _auth.SaveChanges();

        // Seed an Issue reported by the user
        var issue = new Issue("Bug #1", "Something broke", IssuePriority.High, UserId);
        issue.AddComment("First comment", UserId);
        _issues.Issues.Add(issue);
        _issues.SaveChanges();

        // Seed a Notification for the user
        _notifs.Notifications.Add(new Notification(UserId, "alert", "Title", "Body"));
        _notifs.SaveChanges();
    }

    // ── GetDataExport ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDataExport_ExistingUser_ReturnsFullExport()
    {
        var handler = new GetDataExportHandler(_repo);
        var result = await handler.Handle(new GetDataExportQuery(UserId), default);

        result.Should().NotBeNull();
        result!.User.Username.Should().Be("alice");
        result.Issues.Should().HaveCount(1);
        result.Comments.Should().HaveCount(1);
        result.Notifications.Should().HaveCount(1);
        result.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetDataExport_UnknownUser_ReturnsNull()
    {
        var handler = new GetDataExportHandler(_repo);
        var result = await handler.Handle(new GetDataExportQuery(Guid.NewGuid()), default);

        result.Should().BeNull();
    }

    // ── RequestDeletion ───────────────────────────────────────────────────────

    [Fact]
    public async Task RequestDeletion_ValidUser_SoftDeletesAndReturnsDto()
    {
        var emailMock = new Mock<IEmailService>();
        var jobMock = new Mock<IBackgroundJobClient>();
        var handler = new RequestDeletionHandler(
            _repo, emailMock.Object, jobMock.Object,
            NullLogger<RequestDeletionHandler>.Instance);

        var result = await handler.Handle(new RequestDeletionCommand(UserId, AdminId), default);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Pending");
        result.ScheduledHardDeleteAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));

        // User should be soft-deleted
        var user = await _auth.Users.FindAsync(UserId);
        user!.IsActive.Should().BeFalse();

        // Job should have been scheduled
        jobMock.Verify(j => j.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task RequestDeletion_Idempotent_ReturnsSameRequest()
    {
        var emailMock = new Mock<IEmailService>();
        var jobMock = new Mock<IBackgroundJobClient>();
        var handler = new RequestDeletionHandler(
            _repo, emailMock.Object, jobMock.Object,
            NullLogger<RequestDeletionHandler>.Instance);

        var r1 = await handler.Handle(new RequestDeletionCommand(UserId, AdminId), default);
        var r2 = await handler.Handle(new RequestDeletionCommand(UserId, AdminId), default);

        r1!.Id.Should().Be(r2!.Id);
    }

    [Fact]
    public async Task RequestDeletion_UnknownUser_ReturnsNull()
    {
        var emailMock = new Mock<IEmailService>();
        var jobMock = new Mock<IBackgroundJobClient>();
        var handler = new RequestDeletionHandler(
            _repo, emailMock.Object, jobMock.Object,
            NullLogger<RequestDeletionHandler>.Instance);

        var result = await handler.Handle(new RequestDeletionCommand(Guid.NewGuid(), AdminId), default);

        result.Should().BeNull();
    }

    // ── CancelDeletion ────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDeletion_PendingRequest_ReactivatesUser()
    {
        // First create a delete request
        await _repo.CreateDeleteRequestAsync(UserId);

        var handler = new CancelDeletionHandler(_repo, NullLogger<CancelDeletionHandler>.Instance);
        var ok = await handler.Handle(new CancelDeletionCommand(UserId, AdminId), default);

        ok.Should().BeTrue();

        var user = await _auth.Users.FindAsync(UserId);
        user!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CancelDeletion_NoPendingRequest_ReturnsFalse()
    {
        var handler = new CancelDeletionHandler(_repo, NullLogger<CancelDeletionHandler>.Instance);
        var ok = await handler.Handle(new CancelDeletionCommand(UserId, AdminId), default);

        ok.Should().BeFalse();
    }

    // ── GdprHardDeleteJob ─────────────────────────────────────────────────────

    [Fact]
    public async Task HardDeleteJob_DueRequest_DeletesUserData()
    {
        var request = DeleteRequest.Create(UserId, hardDeleteAfterDays: 0); // due immediately
        _auth.DeleteRequests.Add(request);
        await _auth.SaveChangesAsync();

        // Also soft-delete user
        var user = await _auth.Users.FindAsync(UserId);
        user!.IsActive = false;
        await _auth.SaveChangesAsync();

        var job = new GdprHardDeleteJob(_repo, NullLogger<GdprHardDeleteJob>.Instance);
        await job.ExecuteAsync();

        // User should be gone
        var deletedUser = await _auth.Users.FindAsync(UserId);
        deletedUser.Should().BeNull();

        // Notifications should be gone
        var notifCount = await _notifs.Notifications.CountAsync(n => n.UserId == UserId);
        notifCount.Should().Be(0);

        // Issues should be gone
        var issueCount = await _issues.Issues.IgnoreQueryFilters().CountAsync(i => i.ReporterId == UserId);
        issueCount.Should().Be(0);

        // Request should be marked Executed
        var updatedRequest = await _auth.DeleteRequests.FindAsync(request.Id);
        updatedRequest!.Status.Should().Be(DeleteRequestStatus.Executed);
    }

    [Fact]
    public async Task HardDeleteJob_NotDueRequest_SkipsExecution()
    {
        var request = DeleteRequest.Create(UserId, hardDeleteAfterDays: 30); // not due
        _auth.DeleteRequests.Add(request);
        await _auth.SaveChangesAsync();

        var job = new GdprHardDeleteJob(_repo, NullLogger<GdprHardDeleteJob>.Instance);
        await job.ExecuteAsync();

        // User should still exist
        var user = await _auth.Users.FindAsync(UserId);
        user.Should().NotBeNull();

        // Request should still be Pending
        var updatedRequest = await _auth.DeleteRequests.FindAsync(request.Id);
        updatedRequest!.Status.Should().Be(DeleteRequestStatus.Pending);
    }

    public void Dispose()
    {
        _auth.Dispose();
        _issues.Dispose();
        _notifs.Dispose();
    }
}
