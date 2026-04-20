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

    public static async Task InitializeIssuesModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IssuesDbContext>();

        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            var sql = db.Database.GenerateCreateScript();
            foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                catch (Microsoft.Data.Sqlite.SqliteException ex)
                    when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists")) { }
            }
        }
        else
        {
            var script = db.Database.GenerateCreateScript();
            foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07" || ex.SqlState == "42710" || ex.SqlState == "23505") { }
            }
        }
    }
}
