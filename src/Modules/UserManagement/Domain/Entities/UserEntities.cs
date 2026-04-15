using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.UserManagement.Domain.Entities;

// ── Aggregate Root ────────────────────────────────────────────────────────────

public class ManagedUser : BaseEntity
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public string? TimeZone { get; set; }

    public List<ManagedUserRole> UserRoles { get; set; } = [];
}

public class ManagedRole : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public List<ManagedUserRole> UserRoles { get; set; } = [];
}

public class ManagedUserRole
{
    public Guid UserId { get; set; }
    public ManagedUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public ManagedRole Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
