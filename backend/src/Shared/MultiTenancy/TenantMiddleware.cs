using System.Diagnostics;
using IMS.Modular.Shared.MultiTenancy.TenantManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-080: Resolves the current tenant from JWT claim or X-Tenant-Id header.
/// When EnableMultiTenancy is active:
///   - Sets IsMultiTenancyEnabled=true on TenantContext (safe for EF Core filter expressions).
///   - Validates that the tenant exists and is active in the Tenants table.
///   - Returns 403 for unknown or deactivated tenants.
///   - Tags OpenTelemetry Activity with tenant.id for observability.
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Tenant-Id";
    private const string ClaimName  = "tenant_id";
    private const string Fallback   = "default";

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        IFeatureManager featureManager,
        IServiceProvider serviceProvider)
    {
        string? tenantId = null;
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerVal)
            && !string.IsNullOrWhiteSpace(headerVal))
            tenantId = headerVal!;
        if (tenantId is null)
            tenantId = context.User.FindFirst(ClaimName)?.Value;

        tenantId ??= Fallback;

        // Resolve MT flag once (async) then store synchronously in TenantContext.
        // EF Core query filters read IsMultiTenancyEnabled synchronously — no async.
        var mtEnabled = await featureManager.IsEnabledAsync(
            IMS.Modular.Shared.FeatureFlags.FeatureFlags.EnableMultiTenancy);

        tenantContext.SetTenant(tenantId, mtEnabled);

        // US-080: When multi-tenancy is enabled, validate tenant exists and is active.
        if (mtEnabled && tenantId != Fallback)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant is null || !tenant.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Tenant not found or inactive.",
                    tenantId
                });
                return;
            }
        }

        // US-080: Tag OpenTelemetry Activity for trace-per-tenant observability.
        Activity.Current?.SetTag("tenant.id", tenantId);

        await next(context);
    }
}
