using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Domain.Events;
using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Shared.Domain;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.UserManagement.Application.Handlers;

// ── UpdateUser ────────────────────────────────────────────────────────────────

public class UpdateUserCommandHandler(
    IUserManagementRepository repo,
    IUserManagementReadRepository readRepo,
    IPublisher publisher)
    : IRequestHandler<UpdateUserCommand, Result<UserListItemDto>>
{
    public async Task<Result<UserListItemDto>> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<UserListItemDto>.NotFound("User not found.");

        user.FullName = request.FullName;
        user.Department = request.Department;
        user.JobTitle = request.JobTitle;
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.TimeZone = request.TimeZone;
        user.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserProfileUpdatedEvent(user.Id, user.Email, user.FullName), ct);

        var dto = await readRepo.GetByIdAsync(request.UserId, ct);
        return Result<UserListItemDto>.Success(dto!);
    }
}

// ── UpdateUserProfile ─────────────────────────────────────────────────────────

public class UpdateUserProfileCommandHandler(
    IUserManagementRepository repo,
    IUserManagementReadRepository readRepo,
    IPublisher publisher)
    : IRequestHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<UserProfileDto>.NotFound("User not found.");

        user.FullName = request.FullName;
        user.Department = request.Department;
        user.JobTitle = request.JobTitle;
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.TimeZone = request.TimeZone;
        user.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserProfileUpdatedEvent(user.Id, user.Email, user.FullName), ct);

        var dto = await readRepo.GetProfileAsync(request.UserId, ct);
        return Result<UserProfileDto>.Success(dto!);
    }
}

// ── ActivateUser ──────────────────────────────────────────────────────────────

public class ActivateUserCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<ActivateUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        if (user.IsActive)
            return Result<bool>.Conflict("User is already active.");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserActivatedEvent(user.Id, user.Email), ct);

        return Result<bool>.Success(true);
    }
}

// ── DeactivateUser ────────────────────────────────────────────────────────────

public class DeactivateUserCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<DeactivateUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        if (!user.IsActive)
            return Result<bool>.Conflict("User is already inactive.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserDeactivatedEvent(user.Id, user.Email), ct);

        return Result<bool>.Success(true);
    }
}

// ── ChangePassword ────────────────────────────────────────────────────────────

public class ChangePasswordCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result<bool>.Failure("Current password is incorrect.", 400);

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserPasswordChangedEvent(user.Id, user.Email), ct);

        return Result<bool>.Success(true);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;
}

// ── AssignRole ────────────────────────────────────────────────────────────────

public class AssignRoleCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<AssignRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AssignRoleCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        var role = await repo.GetRoleByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result<bool>.NotFound("Role not found.");

        if (user.UserRoles.Any(r => r.RoleId == request.RoleId))
            return Result<bool>.Conflict("User already has this role.");

        user.UserRoles.Add(new Domain.Entities.ManagedUserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role
        });
        user.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserRoleAssignedEvent(user.Id, role.Id, role.Name), ct);

        return Result<bool>.Success(true);
    }
}

// ── RemoveRole ────────────────────────────────────────────────────────────────

public class RemoveRoleCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<RemoveRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveRoleCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        var role = await repo.GetRoleByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result<bool>.NotFound("Role not found.");

        var userRole = user.UserRoles.FirstOrDefault(r => r.RoleId == request.RoleId);
        if (userRole is null)
            return Result<bool>.Failure("User does not have this role.", 400);

        user.UserRoles.Remove(userRole);
        user.UpdatedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new UserRoleRemovedEvent(user.Id, role.Id, role.Name), ct);

        return Result<bool>.Success(true);
    }
}

// ── DeleteUser ────────────────────────────────────────────────────────────────

public class DeleteUserCommandHandler(
    IUserManagementRepository repo,
    IPublisher publisher)
    : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result<bool>.NotFound("User not found.");

        await publisher.Publish(new UserDeletedEvent(user.Id, user.Email), ct);

        await repo.DeleteAsync(user, ct);
        await repo.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
