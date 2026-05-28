namespace IMS.Modular.Modules.Billing.Application.DTOs;

public record PlanDto(
    string Id,
    string Name,
    string Description,
    int? MaxIssuesPerMonth,
    int? MaxUsers,
    int? MaxTenants,
    decimal PriceMonthly,
    bool IsActive);

public record UsageDto(
    string ResourceType,
    int Count,
    int? Limit,
    int Year,
    int Month);

public record SubscriptionDto(
    Guid Id,
    string TenantId,
    string PlanId,
    string Status,
    DateTime StartedAt,
    DateTime? CancelledAt,
    DateTime? CurrentPeriodEnd);

public record SubscriptionSummaryDto(
    SubscriptionDto Subscription,
    PlanDto Plan,
    IReadOnlyList<UsageDto> Usage);
