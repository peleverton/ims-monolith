using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Shared.Common;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

/// <summary>
/// US-064: Read/write contract for UserManagement — delegates to AuthDbContext
/// so we don't duplicate the Users/Roles schema.
/// </summary>
public interface IUserManagementRepository
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<UserDto?> UpdateProfileAsync(Guid id, string fullName, string email, CancellationToken ct = default);
    Task<bool> ChangeRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
    Task<UserDto?> InviteAsync(string username, string email, string fullName, string role, string passwordHash, CancellationToken ct = default);
}
