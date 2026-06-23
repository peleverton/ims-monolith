using IMS.Modular.Modules.AnomalyDetection.Application.Behaviors;
using IMS.Modular.Modules.AnomalyDetection.Application.Services;
using IMS.Modular.Modules.AnomalyDetection.Domain;
using IMS.Modular.Modules.AnomalyDetection.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.AnomalyDetection;

public static class AnomalyDetectionModuleExtensions
{
    public static IServiceCollection AddAnomalyDetectionModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // DbContext
        services.AddDbContext<AnomalyDetectionDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        // Repository
        services.AddScoped<IAnomalyAlertRepository, AnomalyAlertRepository>();

        // Service
        services.AddScoped<AnomalyDetectionService>();

        // Configuration
        services.Configure<AnomalyThreshold>(
            configuration.GetSection("AnomalyDetection"));

        // Decorator: MediatR pipeline behavior for stock movement commands
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AnomalyDetectionBehavior<,>));

        return services;
    }

    public static async Task UseAnomalyDetectionModuleAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnomalyDetectionDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
