using System.Data;
using IMS.Modular.Modules.SmartAssigner.Application.Chain;
using IMS.Modular.Modules.SmartAssigner.Application.Consumers;
using IMS.Modular.Modules.SmartAssigner.Infrastructure;
using Npgsql;

namespace IMS.Modular.Modules.SmartAssigner;

public static class SmartAssignerModuleExtensions
{
    public static IServiceCollection AddSmartAssignerModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Dapper read connection for Smart Assigner queries
        services.AddScoped<ISmartAssignerRepository>(sp =>
            new SmartAssignerRepository(new NpgsqlConnection(connectionString)));

        // Chain of Responsibility handlers (order matters!)
        services.AddScoped<IAssignmentHandler, CheckAvailabilityHandler>();
        services.AddScoped<IAssignmentHandler, CheckSkillsHandler>();
        services.AddScoped<IAssignmentHandler, CheckWorkloadCapacityHandler>();

        // Chain executor
        services.AddScoped<AssignmentChainExecutor>();

        // Background consumer
        services.AddHostedService<SmartAssignerConsumerService>();

        return services;
    }
}
