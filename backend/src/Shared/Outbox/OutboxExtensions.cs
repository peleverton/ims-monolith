using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

    public static async Task InitializeOutboxAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        // For SQLite in-memory or when EnsureCreated succeeds, use that path
        if (db.Database.IsSqlite() || db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        // For Postgres, use raw SQL to create the schema idempotently
        var script = db.Database.GenerateCreateScript();
        foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var sql = statement.Trim();
            if (string.IsNullOrWhiteSpace(sql)) continue;
            try { await db.Database.ExecuteSqlRawAsync(sql); }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07" || ex.SqlState == "42710") { /* already exists */ }
        }
    }
}
