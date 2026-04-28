namespace IMS.Modular.Shared.MultiTenancy;

public class TenantContext : ITenantService
{
    public string? TenantId { get; private set; }
    public bool HasTenant => !string.IsNullOrWhiteSpace(TenantId);

    public void SetTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
    }

    public void Clear() => TenantId = null;
}
