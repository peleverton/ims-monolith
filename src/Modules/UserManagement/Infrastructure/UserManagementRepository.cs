using IMS.Modular.Modules.UserManagement.Domain.Entities;
using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

// ── Write Repository (EF Core) ────────────────────────────────────────────────

public class UserManagementRepository(UserManagementDbContext db) : IUserManagementRepository
{
    public async Task<ManagedUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<ManagedUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<ManagedUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<ManagedRole?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
        => await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);

    public async Task AddAsync(ManagedUser user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public Task DeleteAsync(ManagedUser user, CancellationToken ct = default)
    {
        db.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
