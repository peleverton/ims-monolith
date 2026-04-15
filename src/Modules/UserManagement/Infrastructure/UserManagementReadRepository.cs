using Dapper;
using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Shared.Common;
using System.Data;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

// ── Dapper Read Repository ────────────────────────────────────────────────────

file static class GH
{
    internal static string Up(Guid id) => id.ToString().ToUpperInvariant();
}

public class UserManagementReadRepository(IDbConnection connection) : IUserManagementReadRepository
{
    public async Task<UserListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                   u.Department, u.JobTitle, u.AvatarUrl, u.LastLoginAt, u.CreatedAt,
                   GROUP_CONCAT(r.Name, ',') AS RolesCsv
            FROM ManagedUsers u
            LEFT JOIN ManagedUserRoles ur ON UPPER(ur.UserId) = UPPER(u.Id)
            LEFT JOIN ManagedRoles r      ON UPPER(r.Id)      = UPPER(ur.RoleId)
            WHERE UPPER(u.Id) = @Id
            GROUP BY u.Id
            """;

        var raw = await connection.QuerySingleOrDefaultAsync<UserRaw>(sql, new { Id = GH.Up(id) });
        return raw is null ? null : MapToDto(raw);
    }

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(u.Username LIKE @Search OR u.Email LIKE @Search OR u.FullName LIKE @Search)");
            p.Add("Search", $"%{search}%");
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var countSql = $"SELECT COUNT(*) FROM ManagedUsers u {whereClause}";

        var dataSql = $"""
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                   u.Department, u.JobTitle, u.AvatarUrl, u.LastLoginAt, u.CreatedAt,
                   GROUP_CONCAT(r.Name, ',') AS RolesCsv
            FROM ManagedUsers u
            LEFT JOIN ManagedUserRoles ur ON UPPER(ur.UserId) = UPPER(u.Id)
            LEFT JOIN ManagedRoles r      ON UPPER(r.Id)      = UPPER(ur.RoleId)
            {whereClause}
            GROUP BY u.Id
            ORDER BY u.CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        p.Add("PageSize", pageSize);
        p.Add("Offset", (page - 1) * pageSize);

        var total = await connection.ExecuteScalarAsync<int>(countSql, p);
        var rows = await connection.QueryAsync<UserRaw>(dataSql, p);
        var items = rows.Select(MapToDto).ToList();

        return new PagedResult<UserListItemDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetByRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                   u.Department, u.JobTitle, u.AvatarUrl, u.LastLoginAt, u.CreatedAt,
                   GROUP_CONCAT(r.Name, ',') AS RolesCsv
            FROM ManagedUsers u
            INNER JOIN ManagedUserRoles ur ON UPPER(ur.UserId) = UPPER(u.Id)
            INNER JOIN ManagedRoles r      ON UPPER(r.Id)      = UPPER(ur.RoleId)
            WHERE UPPER(ur.RoleId) = @RoleId
            GROUP BY u.Id
            ORDER BY u.FullName
            """;

        var rows = await connection.QueryAsync<UserRaw>(sql, new { RoleId = GH.Up(roleId) });
        return rows.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetActiveUsersAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                   u.Department, u.JobTitle, u.AvatarUrl, u.LastLoginAt, u.CreatedAt,
                   GROUP_CONCAT(r.Name, ',') AS RolesCsv
            FROM ManagedUsers u
            LEFT JOIN ManagedUserRoles ur ON UPPER(ur.UserId) = UPPER(u.Id)
            LEFT JOIN ManagedRoles r      ON UPPER(r.Id)      = UPPER(ur.RoleId)
            WHERE u.IsActive = 1
            GROUP BY u.Id
            ORDER BY u.FullName
            """;

        var rows = await connection.QueryAsync<UserRaw>(sql);
        return rows.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                   u.AvatarUrl, u.Department, u.JobTitle, u.PhoneNumber,
                   u.Bio, u.TimeZone, u.LastLoginAt, u.CreatedAt, u.UpdatedAt,
                   GROUP_CONCAT(r.Name, ',') AS RolesCsv
            FROM ManagedUsers u
            LEFT JOIN ManagedUserRoles ur ON UPPER(ur.UserId) = UPPER(u.Id)
            LEFT JOIN ManagedRoles r      ON UPPER(r.Id)      = UPPER(ur.RoleId)
            WHERE UPPER(u.Id) = @Id
            GROUP BY u.Id
            """;

        var raw = await connection.QuerySingleOrDefaultAsync<UserProfileRaw>(sql, new { Id = GH.Up(id) });
        if (raw is null) return null;

        var roles = string.IsNullOrEmpty(raw.RolesCsv)
            ? Array.Empty<string>()
            : raw.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        return new UserProfileDto(
            raw.Id, raw.Username, raw.Email, raw.FullName, raw.IsActive,
            raw.AvatarUrl, raw.Department, raw.JobTitle, raw.PhoneNumber,
            raw.Bio, raw.TimeZone, raw.LastLoginAt, raw.CreatedAt, raw.UpdatedAt ?? raw.CreatedAt,
            roles);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static UserListItemDto MapToDto(UserRaw raw)
    {
        var roles = string.IsNullOrEmpty(raw.RolesCsv)
            ? Array.Empty<string>()
            : raw.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        return new UserListItemDto(
            raw.Id, raw.Username, raw.Email, raw.FullName, raw.IsActive,
            raw.Department, raw.JobTitle, raw.AvatarUrl, raw.LastLoginAt, raw.CreatedAt,
            roles);
    }

    // ── Raw projection types ──────────────────────────────────────────────────

    private sealed class UserRaw
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public bool IsActive { get; init; }
        public string? Department { get; init; }
        public string? JobTitle { get; init; }
        public string? AvatarUrl { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? RolesCsv { get; init; }
    }

    private sealed class UserProfileRaw
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public bool IsActive { get; init; }
        public string? AvatarUrl { get; init; }
        public string? Department { get; init; }
        public string? JobTitle { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Bio { get; init; }
        public string? TimeZone { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? RolesCsv { get; init; }
    }
}
