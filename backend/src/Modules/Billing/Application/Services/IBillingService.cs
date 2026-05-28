using IMS.Modular.Modules.Billing.Application.DTOs;
using IMS.Modular.Modules.Billing.Domain.Entities;

namespace IMS.Modular.Modules.Billing.Application.Services;

public interface IBillingService
{
    Task<SubscriptionSummaryDto?> GetSubscriptionAsync(string tenantId);
    Task<bool> CheckQuotaAsync(string tenantId, string resourceType);
    Task IncrementUsageAsync(string tenantId, string resourceType);
    Task<Subscription> GetOrCreateSubscriptionAsync(string tenantId, string planId = "free");
    Task OverridePlanAsync(string tenantId, string planId);
}
