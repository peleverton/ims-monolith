using IMS.Modular.Modules.MarkdownOptimizer.Application.Services;
using IMS.Modular.Modules.MarkdownOptimizer.Domain;
using IMS.Modular.Modules.MarkdownOptimizer.Infrastructure;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.MarkdownOptimizer;

public static class MarkdownOptimizerModuleExtensions
{
    public static IServiceCollection AddMarkdownOptimizerModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // DbContext — reuses the same connection strategy as other modules
        services.AddDbContext<MarkdownOptimizerDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        // Repositories
        services.AddScoped<IMarkdownRuleRepository, MarkdownRuleRepository>();
        services.AddScoped<IMarkdownApplicationRepository, MarkdownApplicationRepository>();

        // Services
        services.AddSingleton<MarkdownEngine>();

        return services;
    }

    /// <summary>
    /// Garante que as tabelas do MarkdownOptimizer existam (auto-migrate em dev).
    /// </summary>
    public static async Task UseMarkdownOptimizerModuleAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarkdownOptimizerDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
