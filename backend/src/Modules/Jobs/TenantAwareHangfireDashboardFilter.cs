using Hangfire.Dashboard;
using IMS.Modular.Shared.MultiTenancy;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-086: Replaces HangfireAdminAuthFilter with tenant-aware filtering.
/// When multi-tenancy is enabled, Admin users only see jobs tagged with their own tenant.
/// When multi-tenancy is disabled, any authenticated Admin sees all jobs.
/// </summary>
internal sealed class TenantAwareHangfireDashboardFilter(ITenantService tenantService) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        if (!httpContext.User.IsInRole("Admin"))
            return false;

        // When multi-tenancy is disabled, any Admin can access the full dashboard
        if (!tenantService.IsMultiTenancyEnabled)
            return true;

        // Multi-tenancy on: only allow access if the request carries a known tenant
        return !string.IsNullOrEmpty(tenantService.TenantId);
    }
}
