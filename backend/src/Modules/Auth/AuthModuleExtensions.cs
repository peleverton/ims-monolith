using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        // JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]
                        ?? throw new InvalidOperationException("JWT SecretKey is not configured"))),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
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

        services.AddAuthorization(options =>
        {
            // Legacy policy — kept for backward compatibility
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            // ── US-057: Granular RBAC policies ────────────────────────────────

            // User management: Admin only
            options.AddPolicy(Policies.CanManageUsers, policy =>
                policy.RequireRole("Admin"));

            // Issues: any authenticated user can create/view
            options.AddPolicy(Policies.CanCreateIssue, policy =>
                policy.RequireAuthenticatedUser());

            // Issues management (delete, bulk): Admin or Manager
            options.AddPolicy(Policies.CanManageIssues, policy =>
                policy.RequireRole("Admin", "Manager"));

            // Inventory read: any authenticated user
            options.AddPolicy(Policies.CanViewInventory, policy =>
                policy.RequireAuthenticatedUser());

            // Inventory write: Admin or Manager
            options.AddPolicy(Policies.CanManageInventory, policy =>
                policy.RequireRole("Admin", "Manager"));

            // Analytics: Admin or Manager
            options.AddPolicy(Policies.CanViewAnalytics, policy =>
                policy.RequireRole("Admin", "Manager"));
        });

        return services;
    }

    /// <summary>
    /// US-065: Usa MigrateAsync (SQLite/PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// </summary>
    public static async Task InitializeAuthModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<AuthDbContext>();
}
