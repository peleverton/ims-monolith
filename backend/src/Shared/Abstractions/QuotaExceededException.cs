namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// US-091: Thrown when a tenant exceeds their plan quota for a resource type.
/// Placed in Shared.Abstractions so ExceptionHandlingMiddleware can reference it
/// without creating a dependency on the Billing module.
/// </summary>
public sealed class QuotaExceededException : Exception
{
    public string ResourceType { get; }
    public string TenantId { get; }

    public QuotaExceededException(string resourceType, string tenantId)
        : base($"Quota exceeded for resource '{resourceType}' on tenant '{tenantId}'. Please upgrade your plan.")
    {
        ResourceType = resourceType;
        TenantId = tenantId;
    }
}
