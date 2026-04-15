using IMS.Modular.Modules.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

public class UserManagementDbContext(DbContextOptions<UserManagementDbContext> options) : DbContext(options)
{
    public DbSet<ManagedUser> Users => Set<ManagedUser>();
    public DbSet<ManagedRole> Roles => Set<ManagedRole>();
    public DbSet<ManagedUserRole> UserRoles => Set<ManagedUserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ManagedUser>(entity =>
        {
            entity.ToTable("ManagedUsers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.TimeZone).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<ManagedRole>(entity =>
        {
            entity.ToTable("ManagedRoles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<ManagedUserRole>(entity =>
        {
            entity.ToTable("ManagedUserRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });
    }
}
