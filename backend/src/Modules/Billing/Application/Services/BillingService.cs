using IMS.Modular.Modules.Billing.Application.DTOs;
using IMS.Modular.Modules.Billing.Domain.Entities;
using IMS.Modular.Modules.Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Billing.Application.Services;

/// <summary>
/// US-091: Core billing service — manages subscriptions, plans, and usage quotas.
/// </summary>
public sealed class BillingService(BillingDbContext db) : IBillingService
{
    public async Task<SubscriptionSummaryDto?> GetSubscriptionAsync(string tenantId)
    {
        var subscription = await db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "active");

        if (subscription is null) return null;

        var now = DateTime.UtcNow;
        var usageRecords = await db.UsageRecords
            .Where(u => u.TenantId == tenantId && u.Year == now.Year && u.Month == now.Month)
            .ToListAsync();

        var usageDtos = usageRecords.Select(u => new UsageDto(
            u.ResourceType,
            u.Count,
            GetLimit(subscription.Plan, u.ResourceType),
            u.Year,
            u.Month)).ToList();

        return new SubscriptionSummaryDto(
            MapSubscription(subscription),
            MapPlan(subscription.Plan),
            usageDtos);
    }

    public async Task<bool> CheckQuotaAsync(string tenantId, string resourceType)
    {
        var subscription = await GetOrCreateSubscriptionAsync(tenantId);
        var plan = subscription.Plan;

        if (resourceType == "issues")
        {
            if (plan.MaxIssuesPerMonth is null) return true; // unlimited

            var now = DateTime.UtcNow;
            var usage = await db.UsageRecords.FirstOrDefaultAsync(u =>
                u.TenantId == tenantId && u.ResourceType == resourceType &&
                u.Year == now.Year && u.Month == now.Month);

            var count = usage?.Count ?? 0;
            return count < plan.MaxIssuesPerMonth.Value;
        }

        return true;
    }

    public async Task IncrementUsageAsync(string tenantId, string resourceType)
    {
        var now = DateTime.UtcNow;
        var usage = await db.UsageRecords.FirstOrDefaultAsync(u =>
            u.TenantId == tenantId && u.ResourceType == resourceType &&
            u.Year == now.Year && u.Month == now.Month);

        if (usage is null)
        {
            usage = new UsageRecord
            {
                TenantId = tenantId,
                ResourceType = resourceType,
                Count = 1,
                Year = now.Year,
                Month = now.Month
            };
            db.UsageRecords.Add(usage);
        }
        else
        {
            usage.Count++;
            usage.LastUpdatedAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task<Subscription> GetOrCreateSubscriptionAsync(string tenantId, string planId = "free")
    {
        var subscription = await db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "active");

        if (subscription is not null) return subscription;

        subscription = new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = "active"
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        await db.Entry(subscription).Reference(s => s.Plan).LoadAsync();
        return subscription;
    }

    public async Task OverridePlanAsync(string tenantId, string planId)
    {
        var plan = await db.Plans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Plan '{planId}' not found.");

        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "active");

        if (subscription is null)
        {
            subscription = new Subscription
            {
                TenantId = tenantId,
                PlanId = planId,
                Status = "active"
            };
            db.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.PlanId = planId;
        }

        await db.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int? GetLimit(Plan plan, string resourceType) => resourceType switch
    {
        "issues" => plan.MaxIssuesPerMonth,
        "users"  => plan.MaxUsers,
        _        => null
    };

    private static SubscriptionDto MapSubscription(Subscription s) => new(
        s.Id, s.TenantId, s.PlanId, s.Status,
        s.StartedAt, s.CancelledAt, s.CurrentPeriodEnd);

    private static PlanDto MapPlan(Plan p) => new(
        p.Id, p.Name, p.Description,
        p.MaxIssuesPerMonth, p.MaxUsers, p.MaxTenants,
        p.PriceMonthly, p.IsActive);
}
