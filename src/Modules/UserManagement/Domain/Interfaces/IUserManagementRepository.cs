using IMS.Modular.Modules.UserManagement.Domain.Entities;
using IMS.Modular.Shared.Common;

namespace IMS.Modular.Modules.UserManagement.Domain.Interfaces;

// ── Write repository (EF Core) ────────────────────────────────────────────────

public interface IUserManagementRepository
{
    Task<ManagedUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ManagedUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ManagedUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<ManagedRole?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default);
    Task AddAsync(ManagedUser user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteAsync(ManagedUser user, CancellationToken ct = default);
}

// ── Read repository (Dapper) ──────────────────────────────────────────────────

public interface IUserManagementReadRepository
{
    Task<UserListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserListItemDto>> GetPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<UserListItemDto>> GetByRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyList<UserListItemDto>> GetActiveUsersAsync(CancellationToken ct = default);
    Task<UserProfileDto?> GetProfileAsync(Guid id, CancellationToken ct = default);
}

// ── DTOs for read side ────────────────────────────────────────────────────────

public record UserListItemDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    string? Department,
    string? JobTitle,
    string? AvatarUrl,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    string? AvatarUrl,
    string? Department,
    string? JobTitle,
    string? PhoneNumber,
    string? Bio,
    string? TimeZone,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> Roles);
