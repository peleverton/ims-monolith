using IMS.Modular.Modules.Issues.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Issues;

public static class IssuesModuleExtensions
{
    public static IServiceCollection AddIssuesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IssuesDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(IssuesDbContext).Assembly.FullName)));

        return services;
    }

    public static async Task InitializeIssuesModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IssuesDbContext>();

        // Generate the CREATE TABLE SQL from the model and execute each statement individually.
        // This is safe when sharing a SQLite file with another DbContext because
        // EnsureCreatedAsync() on the first context only creates its own tables.
        var sql = db.Database.GenerateCreateScript();

        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            try
            {
                await db.Database.ExecuteSqlRawAsync(trimmed);
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists"))
            {
                // Table already exists — safe to ignore
            }
        }
    }
}
