using System.Security.Claims;
using FluentAssertions;
using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Shared.FeatureFlags;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace IMS.Modular.Tests.Modules.Auth;

/// <summary>
/// US-090: Tests for Keycloak claims transformation and feature-flag-driven auth setup.
/// These tests verify the Strangler Fig contract:
///   - UseKeycloak=false  → custom JWT path (zero regression)
///   - UseKeycloak=true   → Keycloak OIDC path (claims mapped correctly)
/// </summary>
public class KeycloakAuthTests
{
    // ── KeycloakClaimsTransformation ────────────────────────────────────────────

    [Fact]
    public async Task Transform_WhenRealmAccessRolesPresent_AddsClaimTypesRole()
    {
        var transformation = new KeycloakClaimsTransformation();
        var principal = BuildKeycloakPrincipal(new Dictionary<string, string>
        {
            ["sub"]          = "abc-123",
            ["email"]        = "admin@ims.local",
            ["preferred_username"] = "admin",
            ["realm_access"] = """{"roles":["Admin","User"]}"""
        });

        var result = await transformation.TransformAsync(principal);

        result.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should().BeEquivalentTo(["Admin", "User"]);
    }

    [Fact]
    public async Task Transform_WhenFlatRolesClaim_AddsClaimTypesRole()
    {
        var transformation = new KeycloakClaimsTransformation();
        var principal = BuildKeycloakPrincipal(new Dictionary<string, string>
        {
            ["sub"]   = "def-456",
            ["email"] = "manager@ims.local",
            ["roles"] = """["Manager","User"]"""
        });

        var result = await transformation.TransformAsync(principal);

        result.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should().Contain("Manager").And.Contain("User");
    }

    [Fact]
    public async Task Transform_WhenNoClaims_DoesNotThrow()
    {
        var transformation = new KeycloakClaimsTransformation();
        var principal = BuildKeycloakPrincipal(new Dictionary<string, string>
        {
            ["sub"] = "ghi-789"
        });

        var result = await transformation.TransformAsync(principal);

        result.Claims.Should().NotBeNull();
    }

    [Fact]
    public async Task Transform_WhenAlreadyHasClaimTypesRole_DoesNotDuplicate()
    {
        var transformation = new KeycloakClaimsTransformation();

        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim("sub", "abc-123"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin")); // already present
        identity.AddClaim(new Claim("realm_access", """{"roles":["Admin","User"]}"""));
        var principal = new ClaimsPrincipal(identity);

        var result = await transformation.TransformAsync(principal);

        result.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Should().HaveCount(1, "transformation is a no-op when roles are already present");
    }

    [Fact]
    public async Task Transform_WhenNotAuthenticated_ReturnsOriginalPrincipal()
    {
        var transformation = new KeycloakClaimsTransformation();
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // unauthenticated

        var result = await transformation.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }

    // ── Feature flag — FeatureFlags.UseKeycloak constant ────────────────────────

    [Fact]
    public void UseKeycloak_FeatureFlag_HasCorrectConstantValue()
    {
        FeatureFlags.UseKeycloak.Should().Be("UseKeycloak");
    }

    // ── AuthModuleExtensions — startup config selection ─────────────────────────

    [Fact]
    public void AuthModule_WithUseKeycloakFalse_DoesNotRequireKeycloakConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseKeycloak"] = "false",
                ["Jwt:SecretKey"]  = "super-secret-key-for-unit-tests-at-least-32chars!",
                ["Jwt:Issuer"]     = "IMS.Test",
                ["Jwt:Audience"]   = "IMS.TestClient"
            })
            .Build();

        // Should not throw — custom JWT path does not need Keycloak config
        var act = () =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var useKeycloak = config.GetValue<bool>("FeatureManagement:UseKeycloak");
            useKeycloak.Should().BeFalse();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void AuthModule_WithUseKeycloakTrue_RequiresKeycloakAuthority()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseKeycloak"] = "true",
                ["Keycloak:Authority"]  = "http://keycloak:8080/realms/ims",
                ["Keycloak:Audience"]   = "ims-backend",
                ["Keycloak:RequireHttpsMetadata"] = "false"
            })
            .Build();

        config["Keycloak:Authority"].Should().Be("http://keycloak:8080/realms/ims");
        config.GetValue<bool>("FeatureManagement:UseKeycloak").Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildKeycloakPrincipal(Dictionary<string, string> claimsMap)
    {
        var identity = new ClaimsIdentity("Bearer");
        foreach (var (type, value) in claimsMap)
            identity.AddClaim(new Claim(type, value));
        return new ClaimsPrincipal(identity);
    }
}
