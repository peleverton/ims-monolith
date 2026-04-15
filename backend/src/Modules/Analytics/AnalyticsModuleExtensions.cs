using System.Data;
using IMS.Modular.Modules.Analytics.Infrastructure;
using Microsoft.Data.Sqlite;

namespace IMS.Modular.Modules.Analytics;

public static class AnalyticsModuleExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Dapper read connection (shared SQLite file)
        services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
        services.AddScoped<IAnalyticsReadRepository, AnalyticsReadRepository>();

        return services;
    }
}
