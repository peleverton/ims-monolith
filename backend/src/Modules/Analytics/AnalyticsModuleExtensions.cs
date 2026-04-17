using System.Data;
using IMS.Modular.Modules.Analytics.Infrastructure;
using Npgsql;

namespace IMS.Modular.Modules.Analytics;

public static class AnalyticsModuleExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Dapper read connection (PostgreSQL)
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
        services.AddScoped<IAnalyticsReadRepository, AnalyticsReadRepository>();

        return services;
    }
}
