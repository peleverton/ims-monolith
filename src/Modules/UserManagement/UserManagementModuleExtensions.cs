using IMS.Modular.Modules.UserManagement.Domain.Interfaces;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace IMS.Modular.Modules.UserManagement;

public static class UserManagementModuleExtensions
{
    public static IServiceCollection AddUserManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        // EF Core — write side
        services.AddDbContext<UserManagementDbContext>(options =>
            options.UseSqlite(
                connectionString,
                b => b.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.FullName)));

        services.AddScoped<IUserManagementRepository, UserManagementRepository>();

        // Dapper — read side
        services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
        services.AddScoped<IUserManagementReadRepository, UserManagementReadRepository>();

        return services;
    }

    public static async Task InitializeUserManagementModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();

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
            catch (Microsoft.Data.Sqlite.SqliteException ex)
                when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists"))
            {
                // Table already exists — safe to ignore
            }
        }
    }
}
