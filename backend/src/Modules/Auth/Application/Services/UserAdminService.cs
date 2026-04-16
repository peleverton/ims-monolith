using IMS.Modular.Modules.Auth.Application.DTOs;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.Auth.Application.Services;

public interface IUserAdminService
{
    Task<PagedResult<UserAdminDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, CancellationToken ct = default);
    Task<UserAdminDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<bool> UpdateUserRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<bool> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
    Task<UserAdminDto?> InviteUserAsync(InviteUserRequest request, CancellationToken ct = default);
}

public sealed class UserAdminService(Infrastructure.AuthDbContext db) : IUserAdminService
{
    public async Task<PagedResult<UserAdminDto>> GetUsersAsync(
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

        return new PagedResult<UserAdminDto>(
            [.. users.Select(ToDto)],
            total,
            page,
            pageSize
        );
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
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

    public async Task<bool> UpdateUserRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;

        var newRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (newRole is null) return false;

        // Remove existing roles and assign the new one
        db.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Add(new UserRole { UserId = userId, RoleId = newRole.Id, Role = newRole });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return false;

        user.IsActive = isActive;
        if (!isActive) user.ClearRefreshToken();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<UserAdminDto?> InviteUserAsync(InviteUserRequest request, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email, ct))
            return null;

        var password = request.TemporaryPassword ?? GenerateTemporaryPassword();
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = HashPassword(password),
            IsActive = true
        };

        db.Users.Add(user);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, ct)
                   ?? await db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);

        if (role is not null)
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private static UserAdminDto ToDto(User u) =>
        new(u.Id.ToString(), u.Username, u.Email, u.FullName,
            u.UserRoles.Select(ur => ur.Role.Name).ToArray(),
            u.IsActive, u.LastLoginAt, u.CreatedAt);

    private static string HashPassword(string password) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        return new string(Enumerable.Range(0, 12)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }
}
