namespace IMS.Modular.Modules.Billing.Domain.Entities;

public class Plan
{
    public string Id { get; set; } = null!;           // "free" | "pro" | "enterprise"
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int? MaxIssuesPerMonth { get; set; }       // null = unlimited
    public int? MaxUsers { get; set; }
    public int? MaxTenants { get; set; }
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = null!;
    public string PlanId { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public string Status { get; set; } = "active";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class UsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = null!;
    public string ResourceType { get; set; } = null!;  // "issues" | "users"
    public int Count { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
