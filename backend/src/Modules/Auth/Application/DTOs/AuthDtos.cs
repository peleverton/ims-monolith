namespace IMS.Modular.Modules.Auth.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Email, string Password, string FullName);

public record RefreshTokenRequest(string RefreshToken);

public record AuthenticationResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Username,
    string Email,
    string[] Roles);

public record UserInfoResponse(
    string Id,
    string Username,
    string Email,
    string FullName,
    string[] Roles,
    DateTime CreatedAt);
