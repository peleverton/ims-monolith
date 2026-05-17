using IMS.Modular.Modules.Audit.Api;
using IMS.Modular.Modules.Audit.Application;
using IMS.Modular.Modules.Audit.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Audit;

/// <summary>
/// US-083: DI registration for the Audit module.
/// </summary>
public static class AuditModuleExtensions
{
    public static IServiceCollection AddAuditModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<AuditDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env);
        });

        services.AddScoped<IAuditService, AuditService>();

        return services;
    }

    public static async Task InitializeAuditModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<AuditDbContext>();

    public static WebApplication MapAuditModule(this WebApplication app)
    {
        AuditModule.Map(app);
        return app;
    }
}
