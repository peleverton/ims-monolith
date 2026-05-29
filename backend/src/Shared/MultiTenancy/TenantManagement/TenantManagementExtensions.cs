using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IMS.Modular.Shared.Database;

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

    /// <summary>
    /// Applies EF Core migrations for the Tenants catalog.
    /// Uses the shared ApplyMigrationsAsync helper which handles shared SQLite files correctly.
    /// </summary>
    public static async Task InitializeTenantDbAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<TenantDbContext>();
}
