namespace IMS.Modular.Modules.Billing.Application.Interfaces;

/// <summary>
/// US-091: Marker interface for MediatR commands that require quota enforcement.
/// Commands implementing this interface will be checked against the tenant's plan limits
/// by QuotaBehavior before the handler executes.
/// </summary>
public interface IRequiresQuota
{
    string ResourceType => "issues";
}
