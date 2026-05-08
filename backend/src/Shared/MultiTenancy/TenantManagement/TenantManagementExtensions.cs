using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-080: Registers TenantDbContext and exposes InitializeTenantDbAsync helper.
/// </summary>
public static class TenantManagementExtensions
{
    public static IServiceCollection AddTenantManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("DefaultConnection is missing.");

        services.AddDbContext<TenantDbContext>(options =>
        {
            if (connStr.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connStr);
            else
                options.UseNpgsql(connStr);
        });

        return services;
    }

    /// <summary>Applies migrations (or EnsureCreated for SQLite) and seeds demo tenants.</summary>
    public static async Task InitializeTenantDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var isSqlite = db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (isSqlite)
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
    }
}
