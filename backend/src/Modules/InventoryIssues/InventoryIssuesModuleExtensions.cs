using System.Data;
using IMS.Modular.Modules.InventoryIssues.Application.Consumers;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Modular.Modules.InventoryIssues;

public static class InventoryIssuesModuleExtensions
{
    public static IServiceCollection AddInventoryIssuesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // US-024: SQLite (dev) or PostgreSQL (staging/prod)
        services.AddDbContext<InventoryIssuesDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env,
                migrationsAssembly: typeof(InventoryIssuesDbContext).Assembly.FullName);
        });

        services.AddScoped<IInventoryIssueRepository, InventoryIssueRepository>();

        // Dapper — read side: IDbConnection resolved by environment at runtime
        services.AddScoped<IDbConnection>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
                return new SqliteConnection(connectionString);
            return new NpgsqlConnection(connectionString);
        });

        services.AddScoped<IInventoryIssueReadRepository, InventoryIssueReadRepository>();

        // US-047: Consumer that creates InventoryIssues from low-stock RabbitMQ events
        services.AddHostedService<LowStockConsumerService>();

        return services;
    }

    /// <summary>
    /// US-065: Usa MigrateAsync (SQLite/PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// </summary>
    public static async Task InitializeInventoryIssuesModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<InventoryIssuesDbContext>();
}
