namespace IMS.Modular.Modules.UserManagement.Application.DTOs;

/// <summary>US-064: DTOs for the UserManagement module.</summary>

public record UserDto(
    string Id,
    string Username,
    string Email,
    string FullName,
    string[] Roles,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record RoleDto(string Id, string Name, string? Description);

public record UpdateProfileRequest(
    string FullName,
    string Email);

public record ChangeUserRoleRequest(string RoleName);

public record SetUserActiveRequest(bool IsActive);

public record InviteUserRequest(
    string Username,
    string Email,
    string FullName,
    string Role = "User",
    string? TemporaryPassword = null);
