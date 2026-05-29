using Hangfire.Dashboard;
using IMS.Modular.Shared.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-086: Replaces HangfireAdminAuthFilter with tenant-aware filtering.
/// When multi-tenancy is enabled, Admin users only see jobs tagged with their own tenant.
/// When multi-tenancy is disabled, any authenticated Admin sees all jobs.
/// ITenantService is resolved per-request from the scoped container to avoid
/// capturing a scoped service in a singleton filter.
/// </summary>
internal sealed class TenantAwareHangfireDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        if (!httpContext.User.IsInRole("Admin"))
            return false;

        var tenantService = httpContext.RequestServices.GetService<ITenantService>();

        // When multi-tenancy is disabled (or service unavailable), any Admin can access the full dashboard
        if (tenantService is null || !tenantService.IsMultiTenancyEnabled)
            return true;

        // Multi-tenancy on: only allow access if the request carries a known tenant
        return !string.IsNullOrEmpty(tenantService.TenantId);
    }
}
