using IMS.Modular.Modules.Billing.Application.DTOs;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Modules.Billing.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Billing.Api;

/// <summary>
/// US-091: Billing API endpoints.
/// </summary>
public static class BillingModule
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/api/billing")
            .WithTags("Billing");

        // Public — list active plans
        group.MapGet("/plans", GetPlans)
            .WithName("GetBillingPlans")
            .AllowAnonymous();

        // Authenticated — get current tenant's subscription summary
        group.MapGet("/subscription", GetSubscription)
            .WithName("GetSubscription")
            .RequireAuthorization();

        // Admin only — override plan for any tenant
        group.MapPost("/admin/override", OverridePlan)
            .WithName("OverridePlan")
            .RequireAuthorization(Policies.AdminOnly);

        // Authenticated — create Stripe checkout session
        group.MapPost("/checkout", CreateCheckout)
            .WithName("CreateBillingCheckout")
            .RequireAuthorization();

        // Anonymous — Stripe webhook handler
        group.MapPost("/webhook", HandleWebhook)
            .WithName("BillingWebhook")
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPlans(BillingDbContext db, CancellationToken ct)
    {
        var plans = await db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new PlanDto(p.Id, p.Name, p.Description,
                p.MaxIssuesPerMonth, p.MaxUsers, p.MaxTenants, p.PriceMonthly, p.IsActive))
            .ToListAsync(ct);
        return Results.Ok(plans);
    }

    private static async Task<IResult> GetSubscription(
        IBillingService billingService,
        ITenantService tenantService,
        CancellationToken ct)
    {
        var tenantId = tenantService.TenantId ?? "default";
        var summary = await billingService.GetSubscriptionAsync(tenantId);

        if (summary is null)
        {
            await billingService.GetOrCreateSubscriptionAsync(tenantId);
            summary = await billingService.GetSubscriptionAsync(tenantId);
        }

        return Results.Ok(summary);
    }

    private static async Task<IResult> OverridePlan(
        [FromBody] OverridePlanRequest request,
        IBillingService billingService,
        CancellationToken ct)
    {
        await billingService.OverridePlanAsync(request.TenantId, request.PlanId);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        IStripeService stripeService,
        ITenantService tenantService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = tenantService.TenantId ?? "default";
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        var url = await stripeService.CreateCheckoutSessionAsync(
            tenantId,
            request.PlanId,
            successUrl: $"{baseUrl}/billing/success",
            cancelUrl: $"{baseUrl}/billing/cancel");

        if (url is null)
            return Results.Problem(
                detail: "Stripe is not configured. Set FeatureManagement:UseStripe=true and provide Stripe credentials.",
                statusCode: StatusCodes.Status501NotImplemented,
                title: "Stripe Not Configured");

        return Results.Ok(new { CheckoutUrl = url });
    }

    private static async Task<IResult> HandleWebhook(
        IStripeService stripeService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = httpContext.Request.Headers["Stripe-Signature"].ToString();

        await stripeService.HandleWebhookAsync(payload, signature);
        return Results.Ok();
    }
}

public record OverridePlanRequest(string TenantId, string PlanId);
public record CreateCheckoutRequest(string PlanId);
