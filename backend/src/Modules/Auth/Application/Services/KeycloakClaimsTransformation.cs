using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace IMS.Modular.Modules.Auth.Application.Services;

/// <summary>
/// US-090: Maps Keycloak-specific claims to the IMS internal claim format.
///
/// Keycloak emits roles inside a JSON object:
///   realm_access  → { "roles": ["Admin", "User"] }
///   resource_access → { "ims-backend": { "roles": ["..."] } }
///
/// This transformation flattens realm_access.roles into individual
/// ClaimTypes.Role claims so that the existing UserContextMiddleware,
/// [Authorize(Roles = "...")] attributes, and authorization policies
/// all continue to work without any modifications.
///
/// The transformation is a no-op when UseKeycloak=false because it is
/// only registered in the DI container when Keycloak is active.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim    = "realm_access";
    private const string ResourceAccessClaim = "resource_access";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        // Only transform once per request — skip if ClaimTypes.Role already exists
        if (principal.HasClaim(c => c.Type == ClaimTypes.Role))
            return Task.FromResult(principal);

        var cloned = principal.Clone();
        var clonedIdentity = (ClaimsIdentity)cloned.Identity!;

        // 1. Extract realm-level roles from realm_access.roles
        var realmAccessJson = principal.FindFirstValue(RealmAccessClaim);
        if (!string.IsNullOrEmpty(realmAccessJson))
        {
            try
            {
                var doc = JsonDocument.Parse(realmAccessJson);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                            clonedIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
            catch (JsonException) { /* malformed claim — ignore */ }
        }

        // 2. Fallback: extract from flat "roles" claim (if Keycloak realm is configured
        //    with a "Realm Roles" protocol mapper that emits roles as a JSON array string)
        var flatRoles = principal.FindFirst("roles")?.Value;
        if (!string.IsNullOrEmpty(flatRoles) && !clonedIdentity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<string[]>(flatRoles);
                foreach (var role in arr ?? [])
                    clonedIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
            catch (JsonException) { /* single value, not an array */ }
        }

        return Task.FromResult(cloned);
    }
}
