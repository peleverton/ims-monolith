using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Hangfire;
using IMS.Modular.Shared.RateLimiting;

namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-080: Minimal API endpoints for Tenant management.
/// All routes require the "Admin" role — tenants are system-level resources.
///
///   GET    /api/tenants
///   GET    /api/tenants/{id}
///   POST   /api/tenants
///   PUT    /api/tenants/{id}
///   DELETE /api/tenants/{id}     (soft-delete: IsActive = false)
/// </summary>
public static class TenantEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (TenantDbContext db) =>
        {
            var tenants = await db.Tenants
                .OrderByDescending(t => t.IsActive)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return Results.Ok(tenants.Select(TenantDto.From));
        })
        .WithName("ListTenants")
        .WithSummary("List all tenants (Admin only)");

        group.MapGet("/{id}", async (string id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            return tenant is null ? Results.NotFound() : Results.Ok(TenantDto.From(tenant));
        })
        .WithName("GetTenant");

        group.MapPost("/", async (CreateTenantRequest req, TenantDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("Id and Name are required.");

            if (await db.Tenants.AnyAsync(t => t.Id == req.Id))
                return Results.Conflict($"Tenant '{req.Id}' already exists.");

            var tenant = new TenantEntity
            {
                Id = req.Id.ToLowerInvariant().Trim(),
                Name = req.Name.Trim(),
                Plan = req.Plan ?? "free",
                ContactEmail = req.ContactEmail,
                Notes = req.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tenants/{tenant.Id}", TenantDto.From(tenant));
        })
        .WithName("CreateTenant");

        group.MapPut("/{id}", async (string id, UpdateTenantRequest req, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name))    tenant.Name = req.Name.Trim();
            if (req.Plan is not null)                    tenant.Plan = req.Plan;
            if (req.ContactEmail is not null)            tenant.ContactEmail = req.ContactEmail;
            if (req.Notes is not null)                   tenant.Notes = req.Notes;
            // US-080: Allow re-activation via PUT
            if (req.IsActive.HasValue)
            {
                tenant.IsActive = req.IsActive.Value;
                if (req.IsActive.Value) tenant.DeactivatedAt = null;
                else tenant.DeactivatedAt ??= DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok(TenantDto.From(tenant));
        })
        .WithName("UpdateTenant");

        group.MapDelete("/{id}", async (string id, TenantDbContext db) =>
        {
            if (id == "default")
                return Results.BadRequest("Cannot deactivate the 'default' tenant.");

            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();

            tenant.IsActive = false;
            tenant.DeactivatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeactivateTenant");

        // ── US-092: Self-service tenant signup (public, rate-limited) ────────

        app.MapPost("/api/tenants/signup", async (
            SignupRequest req,
            TenantDbContext tenantDb,
            AuthDbContext authDb,
            IValidator<SignupRequest> validator,
            IBackgroundJobClient jobClient) =>
        {
            // Validate request body
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            // Generate tenant slug from org name
            var slug = GenerateTenantSlug(req.OrgName);
            if (string.IsNullOrEmpty(slug))
                return Results.BadRequest(new { message = "Org name could not be converted to a valid tenant ID." });

            // Conflict check
            if (await tenantDb.Tenants.AnyAsync(t => t.Id == slug))
                return Results.Conflict(new { message = $"Tenant '{slug}' already exists." });

            // Create tenant (inactive until provisioning completes)
            var tenant = new TenantEntity
            {
                Id = slug,
                Name = req.OrgName.Trim(),
                Plan = req.Plan.ToLowerInvariant(),
                ContactEmail = req.Email.Trim(),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                Notes = "Provisioning"
            };
            tenantDb.Tenants.Add(tenant);

            // Create admin user in AuthDbContext
            var username = BuildUsername(slug);
            if (!await authDb.Users.AnyAsync(u => u.Username == username || u.Email == req.Email))
            {
                var adminRole = await authDb.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                var newUser = new User
                {
                    Username = username,
                    Email = req.Email.Trim(),
                    PasswordHash = HashPassword(req.Password),
                    FullName = req.OrgName.Trim(),
                    IsActive = true,
                    TenantId = slug
                };
                authDb.Users.Add(newUser);
                if (adminRole is not null)
                    authDb.UserRoles.Add(new UserRole
                    {
                        UserId = newUser.Id,
                        RoleId = adminRole.Id
                    });
            }

            await tenantDb.SaveChangesAsync();
            await authDb.SaveChangesAsync();

            // Queue the background provisioning job
            jobClient.Enqueue<TenantProvisioningJob>(job =>
                job.ExecuteAsync(slug, req.Plan.ToLowerInvariant(), req.Email.Trim()));

            return Results.Accepted(null, new
            {
                tenantId = slug,
                message = "Provisioning started. Check your email."
            });
        })
        .WithTags("Tenants")
        .WithName("SignupTenant")
        .WithSummary("US-092: Self-service tenant signup (public)")
        .RequireRateLimiting(RateLimitingExtensions.Policies.Signup)
        .AllowAnonymous();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts an org name to a URL-safe tenant slug:
    /// lowercase, alphanumeric + dashes only, max 50 chars.
    /// </summary>
    private static string GenerateTenantSlug(string orgName)
    {
        var slug = orgName.ToLowerInvariant().Trim();
        // Replace spaces (and sequences of spaces) with a single dash
        slug = Regex.Replace(slug, @"\s+", "-");
        // Remove any character that is not alphanumeric or a dash
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        // Collapse multiple consecutive dashes
        slug = Regex.Replace(slug, @"-{2,}", "-");
        // Trim leading/trailing dashes
        slug = slug.Trim('-');
        return slug.Length > 50 ? slug[..50] : slug;
    }

    /// <summary>
    /// Derives a unique username from the tenant slug (matches ^[a-zA-Z0-9_]+$).
    /// </summary>
    private static string BuildUsername(string slug)
    {
        var sanitised = slug.Replace("-", "_");
        var candidate = $"admin_{sanitised}";
        return candidate.Length > 50 ? candidate[..50] : candidate;
    }

    /// <summary>SHA-256 password hash (same algorithm as AuthenticationService).</summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record TenantDto(
    string Id,
    string Name,
    string? Plan,
    bool IsActive,
    DateTime CreatedAt,
    string? ContactEmail,
    string? Notes)
{
    public static TenantDto From(TenantEntity t) =>
        new(t.Id, t.Name, t.Plan, t.IsActive, t.CreatedAt, t.ContactEmail, t.Notes);
}

public record CreateTenantRequest(
    string Id,
    string Name,
    string? Plan,
    string? ContactEmail,
    string? Notes);

public record UpdateTenantRequest(
    string? Name,
    string? Plan,
    string? ContactEmail,
    string? Notes,
    bool? IsActive = null);
