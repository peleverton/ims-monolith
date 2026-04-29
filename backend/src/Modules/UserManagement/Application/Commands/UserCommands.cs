using IMS.Modular.Modules.UserManagement.Application.DTOs;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Commands;

// ── UpdateProfile ─────────────────────────────────────────────────────────────

public record UpdateProfileCommand(Guid UserId, string FullName, string Email) : IRequest<UserDto?>;

// ── ChangeUserRole ────────────────────────────────────────────────────────────

public record ChangeUserRoleCommand(Guid UserId, string RoleName) : IRequest<bool>;

// ── SetUserActive ─────────────────────────────────────────────────────────────

public record SetUserActiveCommand(Guid UserId, bool IsActive, Guid RequesterId) : IRequest<bool>;

// ── InviteUser ────────────────────────────────────────────────────────────────

public record InviteUserCommand(
    string Username,
    string Email,
    string FullName,
    string Role,
    string? TemporaryPassword) : IRequest<UserDto?>;
