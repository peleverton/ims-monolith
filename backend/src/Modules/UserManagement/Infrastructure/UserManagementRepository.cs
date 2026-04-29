using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

/// <summary>
/// US-064: UserManagement repository — reads and writes Users/Roles via the shared
/// AuthDbContext (same schema, different module responsibility).
/// </summary>
public sealed class UserManagementRepository(AuthDbContext db) : IUserManagementRepository
{
    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        int page, int pageSize, string? search, string? role, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                u.FullName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == role));

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>(
            [.. users.Select(ToDto)],
            total, page, pageSize);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user is null ? null : ToDto(user);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        return await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id.ToString(), r.Name, r.Description))
            .ToListAsync(ct);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<UserDto?> UpdateProfileAsync(
        Guid id, string fullName, string email, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;

        // Check email uniqueness (exclude self)
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != id, ct))
            return null;

        user.FullName = fullName;
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<bool> ChangeRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;

        var newRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (newRole is null) return false;

        db.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Add(new UserRole { UserId = userId, RoleId = newRole.Id, Role = newRole });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return false;

        user.IsActive = isActive;
        if (!isActive) user.ClearRefreshToken();
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<UserDto?> InviteAsync(
        string username, string email, string fullName, string role,
        string passwordHash, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct))
            return null;

        var user = new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHash,
            IsActive = true
        };
        db.Users.Add(user);

        var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == role, ct)
            ?? await db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);
        if (roleEntity is not null)
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleEntity.Id, Role = roleEntity });

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(user.Id, ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static UserDto ToDto(User u) => new(
        u.Id.ToString(),
        u.Username,
        u.Email,
        u.FullName,
        [.. u.UserRoles.Select(ur => ur.Role.Name)],
        u.IsActive,
        u.LastLoginAt,
        u.CreatedAt);
}
