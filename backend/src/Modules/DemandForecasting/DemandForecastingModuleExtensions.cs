using IMS.Modular.Modules.DemandForecasting.Application.Jobs;
using IMS.Modular.Modules.DemandForecasting.Application.Services;
using IMS.Modular.Modules.DemandForecasting.Application.Strategies;
using IMS.Modular.Modules.DemandForecasting.Domain;
using IMS.Modular.Modules.DemandForecasting.Infrastructure;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.DemandForecasting;

public static class DemandForecastingModuleExtensions
{
    public static IServiceCollection AddDemandForecastingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // DbContext
        services.AddDbContext<DemandForecastingDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        // Repository
        services.AddScoped<IDemandForecastRepository, DemandForecastRepository>();

        // Strategy Pattern: registrar todas as estratégias de cálculo
        services.AddSingleton<IDemandStrategy, LinearDemandStrategy>();
        services.AddSingleton<IDemandStrategy, WeightedMovingAvgStrategy>();

        // Services
        services.AddScoped<DemandForecastingService>();

        // Hangfire job
        services.AddScoped<DemandForecastJob>();

        return services;
    }

    /// <summary>
    /// Garante que as tabelas do DemandForecasting existam (auto-migrate em dev).
    /// </summary>
    public static async Task UseDemandForecastingModuleAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DemandForecastingDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
