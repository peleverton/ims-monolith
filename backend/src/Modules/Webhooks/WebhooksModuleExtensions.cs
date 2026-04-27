using IMS.Modular.Modules.Webhooks.Api;
using IMS.Modular.Modules.Webhooks.Application;
using IMS.Modular.Modules.Webhooks.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Webhooks;

/// <summary>
/// US-069: DI registration para o módulo de Webhooks.
/// </summary>
public static class WebhooksModuleExtensions
{
    public static IServiceCollection AddWebhooksModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<WebhooksDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env);
        });

        services.AddScoped<IWebhookRepository, WebhookRepository>();

        // Background consumer — fica em espera na fila RabbitMQ
        services.AddHostedService<WebhookDeliveryConsumer>();

        // HttpClient nomeado com timeout para entregas de webhook
        services.AddHttpClient("webhook", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static async Task InitializeWebhooksModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<WebhooksDbContext>();

    public static IEndpointRouteBuilder MapWebhooksModule(this IEndpointRouteBuilder endpoints)
        => WebhooksModule.Map(endpoints);
}
