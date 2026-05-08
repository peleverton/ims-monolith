namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-080: Extension methods for applying per-tenant WHERE clauses on EF Core IQueryable.
/// Use this instead of relying on EF Core global query filters for multi-tenancy,
/// which can be unreliable due to compiled-query plan caching across DbContext instances.
/// </summary>
public static class TenantQueryExtensions
{
    /// <summary>
    /// Adds a WHERE clause that restricts results to the current tenant.
    /// When multi-tenancy is disabled, the query is returned unmodified.
    /// Records with TenantId = NULL are treated as shared/system records visible to all tenants.
    /// </summary>
    public static IQueryable<T> WithTenantFilter<T>(
        this IQueryable<T> query,
        ITenantService tenantService)
        where T : class, ITenantEntity
    {
        if (!tenantService.IsMultiTenancyEnabled || tenantService.TenantId is null)
            return query;

        var tenantId = tenantService.TenantId;
        return query.Where(e => e.TenantId == null || e.TenantId == tenantId);
    }
}
