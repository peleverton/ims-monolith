using IMS.Modular.Modules.Issues.Application.Consumers;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Issues;

public static class IssuesModuleExtensions
{
    public static IServiceCollection AddIssuesModule(this IServiceCollection services, IConfiguration configuration)
    {
        // US-024: SQLite (dev) or PostgreSQL (staging/prod)
        services.AddDbContext<IssuesDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env,
                migrationsAssembly: typeof(IssuesDbContext).Assembly.FullName);
        });

        // US-048: Consumer that broadcasts SignalR notifications when an issue is created
        services.AddHostedService<IssueCreatedConsumerService>();

        return services;
    }

    /// <summary>
    /// US-065: Usa MigrateAsync (SQLite/PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// </summary>
    public static async Task InitializeIssuesModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<IssuesDbContext>();
}
