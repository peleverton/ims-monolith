using FluentAssertions;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.Handlers;
using IMS.Modular.Modules.UserManagement.Application.Queries;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IMS.Modular.Tests.Modules.UserManagement;

/// <summary>
/// US-064: Unit tests for the UserManagement module handlers.
/// Uses EF InMemory — shares AuthDbContext.
/// </summary>
public class UserManagementHandlerTests : IDisposable
{
    private readonly AuthDbContext _db;
    private readonly IUserManagementRepository _repo;

    // Seeded IDs
    private static readonly Guid AdminId  = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid UserId1  = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserRoleId  = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public UserManagementHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AuthDbContext(options);
        _repo = new UserManagementRepository(_db);

        SeedData();
    }

    private void SeedData()
    {
        var adminRole = new Role { Id = AdminRoleId, Name = "Admin" };
        var userRole  = new Role { Id = UserRoleId,  Name = "User" };
        _db.Roles.AddRange(adminRole, userRole);

        var admin = new User
        {
            Id = AdminId, Username = "admin", Email = "admin@ims.test",
            FullName = "Admin User", PasswordHash = "hash", IsActive = true
        };
        admin.UserRoles.Add(new UserRole { UserId = AdminId, RoleId = AdminRoleId, Role = adminRole });

        var user1 = new User
        {
            Id = UserId1, Username = "alice", Email = "alice@ims.test",
            FullName = "Alice Smith", PasswordHash = "hash", IsActive = true
        };
        user1.UserRoles.Add(new UserRole { UserId = UserId1, RoleId = UserRoleId, Role = userRole });

        _db.Users.AddRange(admin, user1);
        _db.SaveChanges();
    }

    // ── GetUsers ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_NoFilter_ReturnsAll()
    {
        var handler = new GetUsersHandler(_repo);
        var result = await handler.Handle(new GetUsersQuery(1, 20, null, null), default);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsers_SearchFilter_ReturnsMatching()
    {
        var handler = new GetUsersHandler(_repo);
        var result = await handler.Handle(new GetUsersQuery(1, 20, "alice", null), default);

        result.TotalCount.Should().Be(1);
        result.Items[0].Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetUsers_RoleFilter_ReturnsOnlyThatRole()
    {
        var handler = new GetUsersHandler(_repo);
        var result = await handler.Handle(new GetUsersQuery(1, 20, null, "Admin"), default);

        result.TotalCount.Should().Be(1);
        result.Items[0].Roles.Should().Contain("Admin");
    }

    // ── GetUserById ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_Existing_ReturnsDto()
    {
        var handler = new GetUserByIdHandler(_repo);
        var result = await handler.Handle(new GetUserByIdQuery(UserId1), default);

        result.Should().NotBeNull();
        result!.Username.Should().Be("alice");
        result.Email.Should().Be("alice@ims.test");
    }

    [Fact]
    public async Task GetUserById_NotFound_ReturnsNull()
    {
        var handler = new GetUserByIdHandler(_repo);
        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), default);

        result.Should().BeNull();
    }

    // ── GetRoles ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        var handler = new GetRolesHandler(_repo);
        var result = await handler.Handle(new GetRolesQuery(), default);

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Select(r => r.Name).Should().Contain(["Admin", "User"]);
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_Valid_UpdatesAndReturnsDto()
    {
        var handler = new UpdateProfileHandler(_repo);
        var result = await handler.Handle(
            new UpdateProfileCommand(UserId1, "Alice Jones", "alice.jones@ims.test"), default);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Alice Jones");
        result.Email.Should().Be("alice.jones@ims.test");
    }

    [Fact]
    public async Task UpdateProfile_DuplicateEmail_ReturnsNull()
    {
        var handler = new UpdateProfileHandler(_repo);
        // Try to set Alice's email to Admin's email
        var result = await handler.Handle(
            new UpdateProfileCommand(UserId1, "Alice", "admin@ims.test"), default);

        result.Should().BeNull();
    }

    // ── ChangeUserRole ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRole_Valid_ReturnsTrue()
    {
        var handler = new ChangeUserRoleHandler(_repo);
        var ok = await handler.Handle(new ChangeUserRoleCommand(UserId1, "Admin"), default);

        ok.Should().BeTrue();

        var updated = await _repo.GetByIdAsync(UserId1);
        updated!.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task ChangeRole_InvalidRole_ReturnsFalse()
    {
        var handler = new ChangeUserRoleHandler(_repo);
        var ok = await handler.Handle(new ChangeUserRoleCommand(UserId1, "SuperAdmin"), default);

        ok.Should().BeFalse();
    }

    // ── SetUserActive ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUser_Valid_ReturnsTrue()
    {
        var handler = new SetUserActiveHandler(_repo);
        var ok = await handler.Handle(new SetUserActiveCommand(UserId1, false, AdminId), default);

        ok.Should().BeTrue();

        var updated = await _repo.GetByIdAsync(UserId1);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUser_Self_ReturnsFalse()
    {
        var handler = new SetUserActiveHandler(_repo);
        // Requester tries to deactivate themselves
        var ok = await handler.Handle(new SetUserActiveCommand(AdminId, false, AdminId), default);

        ok.Should().BeFalse();
    }

    // ── InviteUser ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteUser_NewUser_CreatesAndReturnsDto()
    {
        var handler = new InviteUserHandler(_repo);
        var result = await handler.Handle(
            new InviteUserCommand("bob", "bob@ims.test", "Bob Marley", "User", "password123"), default);

        result.Should().NotBeNull();
        result!.Username.Should().Be("bob");
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task InviteUser_DuplicateUsername_ReturnsNull()
    {
        var handler = new InviteUserHandler(_repo);
        var result = await handler.Handle(
            new InviteUserCommand("alice", "other@ims.test", "Alice X", "User", null), default);

        result.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
