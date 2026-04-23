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
    /// <summary>US-055: Rotates the refresh token on every use.</summary>
    Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken);
    /// <summary>US-055: Revokes the active refresh token for the current user.</summary>
    Task<bool> LogoutAsync(string refreshToken);
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

        user.RecordLogin();
        return await IssueTokensAsync(user);
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
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id, Role = userRole });

        await db.SaveChangesAsync();
        return await IssueTokensAsync(user);
    }

    /// <summary>
    /// US-055: Refresh token rotation.
    /// 1. Look up the hashed token in RefreshTokens table.
    /// 2. Validate it is active (not revoked, not expired).
    /// 3. Revoke the old token.
    /// 4. Issue a brand-new access + refresh token pair.
    /// </summary>
    public async Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (stored is null || !stored.IsActive)
            return null;

        // Generate the new pair before revoking so we can record the replacement hash
        var (newRawToken, newHash) = GenerateRefreshTokenWithHash();

        stored.Revoke(replacedByHash: newHash);

        var user = stored.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var accessToken = jwtTokenService.GenerateAccessToken(
            user.Id.ToString(), user.Username, user.Email, roles);

        var newToken = RefreshToken.Create(user.Id, newHash, jwtTokenService.GetRefreshTokenExpiration());
        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync();

        return new AuthenticationResponse(
            accessToken, newRawToken,
            jwtTokenService.GetAccessTokenExpiration(),
            user.Username, user.Email, roles);
    }

    /// <summary>US-055: Revoke token on explicit logout.</summary>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (stored is null || !stored.IsActive)
            return false;

        stored.Revoke();
        await db.SaveChangesAsync();
        return true;
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

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Issues a brand-new access + refresh token pair and persists the
    /// hashed refresh token in the RefreshTokens table.
    /// </summary>
    private async Task<AuthenticationResponse> IssueTokensAsync(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

        var accessToken = jwtTokenService.GenerateAccessToken(
            user.Id.ToString(), user.Username, user.Email, roles);

        var (rawRefresh, refreshHash) = GenerateRefreshTokenWithHash();
        var refreshToken = RefreshToken.Create(
            user.Id, refreshHash, jwtTokenService.GetRefreshTokenExpiration());

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new AuthenticationResponse(
            accessToken, rawRefresh,
            jwtTokenService.GetAccessTokenExpiration(),
            user.Username, user.Email, roles);
    }

    private static (string raw, string hash) GenerateRefreshTokenWithHash()
    {
        var rawBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(rawBytes);
        var raw = Convert.ToBase64String(rawBytes);
        return (raw, HashToken(raw));
    }

    /// <summary>SHA-256 hash of a token string (hex-encoded, 64 chars).</summary>
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;
}
