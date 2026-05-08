namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-078/080: Abstraction for reading current tenant context.
/// IsMultiTenancyEnabled is set synchronously by TenantMiddleware (after the
/// async feature-flag check) so it can be safely accessed from EF Core query
/// filter expressions without async/await.
/// </summary>
public interface ITenantService
{
    string? TenantId { get; }
    /// <summary>True when the EnableMultiTenancy feature flag is active for this request.</summary>
    bool IsMultiTenancyEnabled { get; }
}
