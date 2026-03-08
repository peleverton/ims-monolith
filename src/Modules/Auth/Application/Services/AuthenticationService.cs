using IMS.Modular.Modules.Auth.Application.DTOs;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Modular.Modules.Auth.Application.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResponse?> LoginAsync(LoginRequest request);
    Task<AuthenticationResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken);
    Task<UserInfoResponse?> GetUserInfoAsync(string username);
}

public sealed class AuthenticationService(AuthDbContext db, JwtTokenService jwtTokenService)
    : IAuthenticationService
{
    public async Task<AuthenticationResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        if (!user.IsActive)
            return null;

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthenticationResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            return null;

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FullName = request.FullName,
            IsActive = true
        };

        db.Users.Add(user);

        // Assign default "User" role
        var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole is not null)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id, Role = userRole });
        }

        await db.SaveChangesAsync();
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return null;

        return await GenerateAuthResponse(user);
    }

    public async Task<UserInfoResponse?> GetUserInfoAsync(string username)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
            return null;

        return new UserInfoResponse(
            user.Id.ToString(), user.Username, user.Email, user.FullName,
            user.UserRoles.Select(ur => ur.Role.Name).ToArray(),
            user.CreatedAt);
    }

    private async Task<AuthenticationResponse> GenerateAuthResponse(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

        var accessToken = jwtTokenService.GenerateAccessToken(
            user.Id.ToString(), user.Username, user.Email, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, jwtTokenService.GetRefreshTokenExpiration());
        user.RecordLogin();
        await db.SaveChangesAsync();

        return new AuthenticationResponse(
            accessToken, refreshToken,
            jwtTokenService.GetAccessTokenExpiration(),
            user.Username, user.Email, roles);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
