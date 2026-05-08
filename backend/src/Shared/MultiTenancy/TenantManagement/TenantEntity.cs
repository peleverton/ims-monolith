namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-080: Represents an IMS tenant (organisation).
/// Stored in the shared "Tenants" table.
/// </summary>
public class TenantEntity
{
    public string Id { get; set; } = null!;         // slug, e.g. "acme-corp"
    public string Name { get; set; } = null!;
    public string? Plan { get; set; }               // "free" | "starter" | "pro"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes { get; set; }
}
