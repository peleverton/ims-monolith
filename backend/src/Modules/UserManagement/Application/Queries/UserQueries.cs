using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Shared.Common;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Queries;

// ── GetUsers ─────────────────────────────────────────────────────────────────

public record GetUsersQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Role) : IRequest<PagedResult<UserDto>>;

// ── GetUserById ───────────────────────────────────────────────────────────────

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;

// ── GetRoles ──────────────────────────────────────────────────────────────────

public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
