namespace IMS.Modular.Modules.Billing.Application.Services;

public interface IStripeService
{
    Task<string?> CreateCheckoutSessionAsync(string tenantId, string planId, string successUrl, string cancelUrl);
    Task<string?> CreateCustomerPortalSessionAsync(string stripeCustomerId, string returnUrl);
    Task HandleWebhookAsync(string payload, string signature);
}

public sealed class NoOpStripeService : IStripeService
{
    public Task<string?> CreateCheckoutSessionAsync(string tenantId, string planId, string successUrl, string cancelUrl)
        => Task.FromResult<string?>(null);

    public Task<string?> CreateCustomerPortalSessionAsync(string stripeCustomerId, string returnUrl)
        => Task.FromResult<string?>(null);

    public Task HandleWebhookAsync(string payload, string signature)
        => Task.CompletedTask;
}
