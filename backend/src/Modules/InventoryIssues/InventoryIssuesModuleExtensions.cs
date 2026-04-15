using System.Data;
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

        return services;
    }

    public static async Task InitializeInventoryIssuesModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryIssuesDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (env.IsDevelopment())
        {
            var sql = db.Database.GenerateCreateScript();
            foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                catch (SqliteException ex)
                    when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists")) { }
            }
        }
        else
        {
            await db.Database.MigrateAsync();
        }
    }
}
