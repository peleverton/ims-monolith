using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Modules.UserManagement.Application.Queries;
using IMS.Modular.Modules.UserManagement.Domain.Events;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using IMS.Modular.Shared.Common;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.UserManagement.Application.Handlers;

// ── Query Handlers ────────────────────────────────────────────────────────────

public sealed class GetUsersHandler(IUserManagementRepository repo)
    : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public Task<PagedResult<UserDto>> Handle(GetUsersQuery q, CancellationToken ct)
        => repo.GetUsersAsync(q.Page, q.PageSize, q.Search, q.Role, ct);
}

public sealed class GetUserByIdHandler(IUserManagementRepository repo)
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public Task<UserDto?> Handle(GetUserByIdQuery q, CancellationToken ct)
        => repo.GetByIdAsync(q.UserId, ct);
}

public sealed class GetRolesHandler(IUserManagementRepository repo)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery _, CancellationToken ct)
        => repo.GetRolesAsync(ct);
}

// ── Command Handlers ──────────────────────────────────────────────────────────

public sealed class UpdateProfileHandler(IUserManagementRepository repo)
    : IRequestHandler<UpdateProfileCommand, UserDto?>
{
    public Task<UserDto?> Handle(UpdateProfileCommand cmd, CancellationToken ct)
        => repo.UpdateProfileAsync(cmd.UserId, cmd.FullName, cmd.Email, ct);
}

public sealed class ChangeUserRoleHandler(IUserManagementRepository repo)
    : IRequestHandler<ChangeUserRoleCommand, bool>
{
    public Task<bool> Handle(ChangeUserRoleCommand cmd, CancellationToken ct)
        => repo.ChangeRoleAsync(cmd.UserId, cmd.RoleName, ct);
}

public sealed class SetUserActiveHandler(IUserManagementRepository repo)
    : IRequestHandler<SetUserActiveCommand, bool>
{
    public async Task<bool> Handle(SetUserActiveCommand cmd, CancellationToken ct)
    {
        if (cmd.UserId == cmd.RequesterId)
            return false; // cannot deactivate yourself

        return await repo.SetActiveAsync(cmd.UserId, cmd.IsActive, ct);
    }
}

public sealed class InviteUserHandler(IUserManagementRepository repo)
    : IRequestHandler<InviteUserCommand, UserDto?>
{
    public Task<UserDto?> Handle(InviteUserCommand cmd, CancellationToken ct)
    {
        var passwordHash = HashPassword(cmd.TemporaryPassword ?? GeneratePassword());
        return repo.InviteAsync(cmd.Username, cmd.Email, cmd.FullName, cmd.Role, passwordHash, ct);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GeneratePassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToBase64String(bytes);
    }
}
