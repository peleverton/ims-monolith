using FluentAssertions;
using IMS.Modular.Modules.Auth.Application.DTOs;
using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IMS.Modular.Tests.Modules.Auth;

/// <summary>
/// US-052: Unit tests for Auth services — AuthenticationService and JwtTokenService.
/// Pattern: AAA (Arrange / Act / Assert)
/// Uses EF InMemory for AuthDbContext.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly AuthDbContext _db;
    private readonly JwtTokenService _jwtService;
    private readonly AuthenticationService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AuthDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "super-secret-key-for-unit-tests-32-chars!",
                ["Jwt:Issuer"] = "IMS.Test",
                ["Jwt:Audience"] = "IMS.TestClient",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshExpirationDays"] = "7"
            })
            .Build();

        _jwtService = new JwtTokenService(config);
        _authService = new AuthenticationService(_db, _jwtService);

        SeedDatabase();
    }

    public void Dispose() => _db.Dispose();

    private void SeedDatabase()
    {
        var userRole = new Role { Name = "User", Description = "Default user role" };
        _db.Roles.Add(userRole);
        _db.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    // JwtTokenService
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ValidInputs_ReturnsNonEmptyToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var token = _jwtService.GenerateAccessToken(userId, "testuser", "test@test.com", ["User"]);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3, "JWT must have 3 parts separated by dots");
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string username = "trinity";
        const string email = "trinity@ims.com";

        // Act
        var token = _jwtService.GenerateAccessToken(userId, username, email, ["Admin", "User"]);

        // Assert — decode without validation to inspect claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // ClaimTypes.NameIdentifier maps to the long URI claim, not the short "sub"
        jwt.Claims.Should().Contain(c => c.Value == userId);
        jwt.Claims.Should().Contain(c => c.Value == username);
        jwt.Claims.Should().Contain(c => c.Value == email);
        jwt.Claims.Should().Contain(c => c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Value == "User");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetAccessTokenExpiration_IsInFuture()
    {
        var expiration = _jwtService.GetAccessTokenExpiration();
        expiration.Should().BeAfter(DateTime.UtcNow);
    }

    // ────────────────────────────────────────────────────────────────
    // AuthenticationService — Register
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewUser_ReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "Password123!", "New User");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("newuser");
        result.Email.Should().Be("new@test.com");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsNull()
    {
        // Arrange
        var request = new RegisterRequest("duplicateuser", "first@test.com", "Password123!", "First");
        await _authService.RegisterAsync(request);

        var duplicate = new RegisterRequest("duplicateuser", "second@test.com", "Password123!", "Second");

        // Act
        var result = await _authService.RegisterAsync(duplicate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        // Arrange
        var request = new RegisterRequest("user1", "same@test.com", "Password123!", "User One");
        await _authService.RegisterAsync(request);

        var duplicate = new RegisterRequest("user2", "same@test.com", "Password123!", "User Two");

        // Act
        var result = await _authService.RegisterAsync(duplicate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Register_PersistsUserToDatabase()
    {
        // Arrange
        var request = new RegisterRequest("persisted", "persisted@test.com", "Password123!", "Persisted User");

        // Act
        await _authService.RegisterAsync(request);

        // Assert
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == "persisted");
        user.Should().NotBeNull();
        user!.Email.Should().Be("persisted@test.com");
        user.IsActive.Should().BeTrue();
    }

    // ────────────────────────────────────────────────────────────────
    // AuthenticationService — Login
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest("loginuser", "login@test.com", "Correct123!", "Login User"));

        // Act
        var result = await _authService.LoginAsync(new LoginRequest("loginuser", "Correct123!"));

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("loginuser");
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest("wrongpass", "wp@test.com", "Correct123!", "Wrong Pass"));

        // Act
        var result = await _authService.LoginAsync(new LoginRequest("wrongpass", "WrongPassword!"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginAsync(new LoginRequest("ghost", "Password123!"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsNull()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest("inactive", "inactive@test.com", "Password123!", "Inactive"));
        var user = await _db.Users.FirstAsync(u => u.Username == "inactive");
        user.IsActive = false;
        await _db.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(new LoginRequest("inactive", "Password123!"));

        // Assert
        result.Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────
    // AuthenticationService — RefreshToken
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewAuthResponse()
    {
        // Arrange
        var registered = await _authService.RegisterAsync(new RegisterRequest("refreshuser", "refresh@test.com", "Pass123!", "Refresh User"));
        registered.Should().NotBeNull();
        var refreshToken = registered!.RefreshToken;

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("refreshuser");
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsNull()
    {
        // Act
        var result = await _authService.RefreshTokenAsync("invalid-token-xyz");

        // Assert
        result.Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────
    // AuthenticationService — GetUserInfo
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserInfo_ExistingUser_ReturnsUserInfo()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest("infouser", "info@test.com", "Pass123!", "Info User"));

        // Act
        var result = await _authService.GetUserInfoAsync("infouser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("infouser");
        result.Email.Should().Be("info@test.com");
        result.FullName.Should().Be("Info User");
    }

    [Fact]
    public async Task GetUserInfo_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserInfoAsync("nobody");

        // Assert
        result.Should().BeNull();
    }
}
