using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023 / US-024: Extensões de DI para o padrão Outbox.
/// </summary>
public static class OutboxExtensions
{
    public static IServiceCollection AddImsOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // US-024: provider selecionado por ambiente (SQLite dev / PostgreSQL prod)
        services.AddDbContext<OutboxDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env);
        });

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddScoped<IOutboxService, OutboxService>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
