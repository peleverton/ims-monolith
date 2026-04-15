using IMS.Modular.Modules.UserManagement.Application.Queries;
using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Shared.Common;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Handlers;

// ── GetPagedUsers ─────────────────────────────────────────────────────────────

public class GetPagedUsersQueryHandler(IUserManagementReadRepository readRepo)
    : IRequestHandler<GetPagedUsersQuery, PagedResult<UserListItemDto>>
{
    public async Task<PagedResult<UserListItemDto>> Handle(GetPagedUsersQuery request, CancellationToken ct)
        => await readRepo.GetPagedAsync(request.Page, request.PageSize, request.Search, ct);
}

// ── GetUserById ───────────────────────────────────────────────────────────────

public class GetUserByIdQueryHandler(IUserManagementReadRepository readRepo)
    : IRequestHandler<GetUserByIdQuery, UserListItemDto?>
{
    public async Task<UserListItemDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
        => await readRepo.GetByIdAsync(request.UserId, ct);
}

// ── GetCurrentUserProfile ─────────────────────────────────────────────────────

public class GetCurrentUserProfileQueryHandler(IUserManagementReadRepository readRepo)
    : IRequestHandler<GetCurrentUserProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetCurrentUserProfileQuery request, CancellationToken ct)
        => await readRepo.GetProfileAsync(request.UserId, ct);
}

// ── GetUsersByRole ────────────────────────────────────────────────────────────

public class GetUsersByRoleQueryHandler(IUserManagementReadRepository readRepo)
    : IRequestHandler<GetUsersByRoleQuery, IReadOnlyList<UserListItemDto>>
{
    public async Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersByRoleQuery request, CancellationToken ct)
        => await readRepo.GetByRoleAsync(request.RoleId, ct);
}

// ── GetActiveUsers ────────────────────────────────────────────────────────────

public class GetActiveUsersQueryHandler(IUserManagementReadRepository readRepo)
    : IRequestHandler<GetActiveUsersQuery, IReadOnlyList<UserListItemDto>>
{
    public async Task<IReadOnlyList<UserListItemDto>> Handle(GetActiveUsersQuery request, CancellationToken ct)
        => await readRepo.GetActiveUsersAsync(ct);
}
