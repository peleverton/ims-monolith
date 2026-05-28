using IMS.Modular.Modules.Billing.Application.Interfaces;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;

namespace IMS.Modular.Modules.Billing.Application.Behaviors;

/// <summary>
/// US-091: MediatR pipeline behavior that enforces billing quota on commands
/// that implement IRequiresQuota. Runs after ValidationBehavior.
/// </summary>
public sealed class QuotaBehavior<TRequest, TResponse>(
    ITenantService tenantService,
    IBillingService billingService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequiresQuota quotaRequest)
            return await next();

        var tenantId = tenantService.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return await next();

        var allowed = await billingService.CheckQuotaAsync(tenantId, quotaRequest.ResourceType);
        if (!allowed)
            throw new QuotaExceededException(quotaRequest.ResourceType, tenantId);

        return await next();
    }
}
