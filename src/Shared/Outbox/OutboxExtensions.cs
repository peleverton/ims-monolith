using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Extensões de DI para o padrão Outbox.
/// </summary>
public static class OutboxExtensions
{
    /// <summary>
    /// Registra o OutboxDbContext, IOutboxService e OutboxProcessor (background service).
    /// Usa o mesmo banco SQLite do projeto (connection string "DefaultConnection").
    /// </summary>
    public static IServiceCollection AddImsOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=ims-monolith.db";

        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddScoped<IOutboxService, OutboxService>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
