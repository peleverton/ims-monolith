using System.Data;
using IMS.Modular.Modules.WarehouseRouting.Application.Strategies;
using IMS.Modular.Modules.WarehouseRouting.Infrastructure;
using Npgsql;

namespace IMS.Modular.Modules.WarehouseRouting;

public static class WarehouseRoutingModuleExtensions
{
    public static IServiceCollection AddWarehouseRoutingModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Repository
        services.AddScoped<IWarehouseGraphRepository>(sp =>
            new WarehouseGraphRepository(new NpgsqlConnection(connectionString)));

        // Strategy Pattern: register all routing strategies
        services.AddSingleton<IRoutingStrategy, DijkstraRoutingStrategy>();
        services.AddSingleton<IRoutingStrategy, AStarRoutingStrategy>();

        return services;
    }
}
