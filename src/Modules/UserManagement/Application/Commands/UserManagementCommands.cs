using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Shared.Domain;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Commands;

// ── US-019: User Management Commands ─────────────────────────────────────────

public record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string? Department,
    string? JobTitle,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? TimeZone) : IRequest<Result<UserListItemDto>>;

public record UpdateUserProfileCommand(
    Guid UserId,
    string FullName,
    string? Department,
    string? JobTitle,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? TimeZone) : IRequest<Result<UserProfileDto>>;

public record ActivateUserCommand(Guid UserId) : IRequest<Result<bool>>;

public record DeactivateUserCommand(Guid UserId) : IRequest<Result<bool>>;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Result<bool>>;

public record AssignRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result<bool>>;

public record RemoveRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result<bool>>;

public record DeleteUserCommand(Guid UserId) : IRequest<Result<bool>>;
