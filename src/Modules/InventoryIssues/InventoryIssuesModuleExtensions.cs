using System.Data;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.InventoryIssues;

public static class InventoryIssuesModuleExtensions
{
    public static IServiceCollection AddInventoryIssuesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<InventoryIssuesDbContext>(options =>
            options.UseSqlite(connectionString,
                b => b.MigrationsAssembly(typeof(InventoryIssuesDbContext).Assembly.FullName)));

        services.AddScoped<IInventoryIssueRepository, InventoryIssueRepository>();

        services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
        services.AddScoped<IInventoryIssueReadRepository, InventoryIssueReadRepository>();

        return services;
    }

    public static async Task InitializeInventoryIssuesModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryIssuesDbContext>();

        var sql = db.Database.GenerateCreateScript();

        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            try
            {
                await db.Database.ExecuteSqlRawAsync(trimmed);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists"))
            {
                // Safe to ignore — table already exists
            }
        }
    }
}
