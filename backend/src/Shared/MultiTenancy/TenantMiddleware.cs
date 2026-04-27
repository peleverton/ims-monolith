namespace IMS.Modular.Shared.MultiTenancy;

public class TenantMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Tenant-Id";
    private const string ClaimName  = "tenant_id";
    private const string Fallback   = "default";

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        string? tenantId = null;
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerVal)
            && !string.IsNullOrWhiteSpace(headerVal))
            tenantId = headerVal!;
        if (tenantId is null)
            tenantId = context.User.FindFirst(ClaimName)?.Value;
        tenantContext.SetTenant(tenantId ?? Fallback);
        await next(context);
    }
}
