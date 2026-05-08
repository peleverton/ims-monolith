using IMS.Modular.Shared.MultiTenancy.TenantManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

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
    string? Notes);
