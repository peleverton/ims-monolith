using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Database;
using IMS.Modular.Shared.FeatureFlags;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ImsAuthService = IMS.Modular.Modules.Auth.Application.Services.IAuthenticationService;
using ImsAuthServiceImpl = IMS.Modular.Modules.Auth.Application.Services.AuthenticationService;

namespace IMS.Modular.Modules.Auth;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext — US-024: SQLite (dev) or PostgreSQL (staging/prod)
        services.AddDbContext<AuthDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env,
                migrationsAssembly: typeof(AuthDbContext).Assembly.FullName);
        });

        // Services
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<ImsAuthService, ImsAuthServiceImpl>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        // US-090: Feature flag read at startup — determines which auth provider is active.
        // IFeatureManager is async/scoped and cannot be used here at registration time,
        // so we read from IConfiguration directly (same source of truth).
        var useKeycloak = configuration.GetValue<bool>($"FeatureManagement:{FeatureFlags.UseKeycloak}");

        if (useKeycloak)
            ConfigureKeycloakAuth(services, configuration);
        else
            ConfigureCustomJwtAuth(services, configuration);

        ConfigureAuthorizationPolicies(services);

        return services;
    }

    /// <summary>
    /// US-090: Keycloak OIDC — JWT Bearer validation against Keycloak's JWKS endpoint.
    /// Tokens are issued by Keycloak (RSA-signed); the backend only validates them.
    /// Claims transformation maps realm_access.roles → ClaimTypes.Role.
    /// </summary>
    private static void ConfigureKeycloakAuth(IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority is not configured");
        var audience = configuration["Keycloak:Audience"] ?? "ims-backend";
        var requireHttps = configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata", true);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority             = authority;
            options.Audience              = audience;
            options.RequireHttpsMetadata  = requireHttps;
            options.MapInboundClaims      = false; // keep raw claim names from Keycloak

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer   = true,
                ValidateAudience = true,
                ValidAudience    = audience,
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.Zero,
                // Keycloak uses preferred_username for the Name claim
                NameClaimType    = "preferred_username",
                // Roles come from KeycloakClaimsTransformation (realm_access.roles)
                RoleClaimType    = System.Security.Claims.ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                        context.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                }
            };
        });

        // Register claims transformation — maps Keycloak realm_access.roles → ClaimTypes.Role
        services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformation>();
    }

    /// <summary>
    /// Original custom JWT auth (HMAC-SHA256, symmetric key).
    /// Preserved as fallback when UseKeycloak=false — zero regression.
    /// </summary>
    private static void ConfigureCustomJwtAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]
                        ?? throw new InvalidOperationException("JWT SecretKey is not configured"))),
                ValidateIssuer   = true,
                ValidIssuer      = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience    = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                        context.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                }
            };
        });
    }

    private static void ConfigureAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole("Admin"));

            // ── US-057: Granular RBAC policies ────────────────────────────────

            options.AddPolicy(Policies.CanManageUsers, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(Policies.CanCreateIssue, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(Policies.CanManageIssues, policy =>
                policy.RequireRole("Admin", "Manager"));

            options.AddPolicy(Policies.CanViewInventory, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(Policies.CanManageInventory, policy =>
                policy.RequireRole("Admin", "Manager"));

            options.AddPolicy(Policies.CanViewAnalytics, policy =>
                policy.RequireRole("Admin", "Manager"));
        });
    }

    /// <summary>
    /// US-065: Usa MigrateAsync (SQLite/PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// </summary>
    public static async Task InitializeAuthModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<AuthDbContext>();
}
