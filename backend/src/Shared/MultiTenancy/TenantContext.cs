namespace IMS.Modular.Shared.MultiTenancy;

public class TenantContext : ITenantService
{
    public string? TenantId { get; private set; }
    public bool HasTenant => !string.IsNullOrWhiteSpace(TenantId);

    /// <summary>
    /// US-080: Set synchronously by TenantMiddleware after resolving the feature flag.
    /// Safe to use inside EF Core query filter expressions (no async calls needed).
    /// </summary>
    public bool IsMultiTenancyEnabled { get; private set; }

    public void SetTenant(string tenantId, bool multiTenancyEnabled = false)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        IsMultiTenancyEnabled = multiTenancyEnabled;
    }

    public void Clear()
    {
        TenantId = null;
        IsMultiTenancyEnabled = false;
    }
}
