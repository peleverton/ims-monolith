using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Common;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Queries;

// ── US-019: User Management Queries ──────────────────────────────────────────

public record GetPagedUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<PagedResult<UserListItemDto>>, ICacheable
{
    public string CacheKeyPrefix => "users-paged";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetUserByIdQuery(Guid UserId) : IRequest<UserListItemDto?>, ICacheable
{
    public string CacheKeyPrefix => "user-by-id";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetCurrentUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>, ICacheable
{
    public string CacheKeyPrefix => "user-profile";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetUsersByRoleQuery(Guid RoleId) : IRequest<IReadOnlyList<UserListItemDto>>, ICacheable
{
    public string CacheKeyPrefix => "users-by-role";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetActiveUsersQuery() : IRequest<IReadOnlyList<UserListItemDto>>, ICacheable
{
    public string CacheKeyPrefix => "users-active";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
