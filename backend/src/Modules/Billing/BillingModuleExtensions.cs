using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Modules.Billing.Infrastructure;
using IMS.Modular.Shared.Database;
using IMS.Modular.Shared.FeatureFlags;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Billing;

/// <summary>
/// US-091: DI registration and initialization for the Billing module.
/// </summary>
public static class BillingModuleExtensions
{
    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env);
        });

        services.AddScoped<IBillingService, BillingService>();

        // Register Stripe service — real or no-op based on feature flag.
        // Feature flag is read from configuration at startup (IFeatureManager is scoped/async
        // and cannot be used at registration time, so we read IConfiguration directly).
        var useStripe = configuration.GetValue<bool>($"FeatureManagement:{FeatureFlags.UseStripe}");
        if (useStripe)
        {
            // Real Stripe integration would be registered here (e.g., StripeService).
            // For now, fall through to NoOp since Stripe SDK is not yet a dependency.
            services.AddScoped<IStripeService, NoOpStripeService>();
        }
        else
        {
            services.AddScoped<IStripeService, NoOpStripeService>();
        }

        return services;
    }

    public static async Task InitializeBillingModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<BillingDbContext>();
}
