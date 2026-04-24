using Hangfire;
using Hangfire.Dashboard;
using Hangfire.InMemory;
using Hangfire.PostgreSql;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Background Jobs com Hangfire.
/// - Development: storage InMemory (zero infra)
/// - Production: storage PostgreSQL (persistência, retry automático)
/// </summary>
public static class JobsModuleExtensions
{
    public static IServiceCollection AddJobsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Registrar os jobs como serviços
        services.AddScoped<ExpiryCheckJob>();
        services.AddScoped<OverdueIssuesJob>();
        services.AddScoped<AnalyticsSnapshotJob>();
        services.AddScoped<TokenCleanupJob>();

        // Configurar Hangfire storage
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            if (environment.IsDevelopment())
            {
                config.UseInMemoryStorage();
            }
            else
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");

                config.UsePostgreSqlStorage(opt =>
                    opt.UseNpgsqlConnection(connectionString));
            }
        });

        services.AddHangfireServer(opt =>
        {
            opt.WorkerCount = environment.IsDevelopment() ? 2 : 5;
            opt.Queues = ["default", "critical"];
        });

        return services;
    }

    public static void UseJobsModule(this WebApplication app)
    {
        // Dashboard Hangfire — apenas Admin (policy CanManageUsers)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAdminAuthFilter()],
            AppPath = "/",
            DashboardTitle = "IMS — Background Jobs",
        });

        // Registrar jobs recorrentes
        var manager = app.Services.GetRequiredService<IRecurringJobManager>();

        manager.AddOrUpdate<ExpiryCheckJob>(
            "expiry-check",
            job => job.ExecuteAsync(),
            Cron.Daily());

        manager.AddOrUpdate<OverdueIssuesJob>(
            "overdue-issues",
            job => job.ExecuteAsync(),
            "0 */6 * * *");

        manager.AddOrUpdate<AnalyticsSnapshotJob>(
            "analytics-snapshot",
            job => job.ExecuteAsync(),
            Cron.Weekly());

        manager.AddOrUpdate<TokenCleanupJob>(
            "token-cleanup",
            job => job.ExecuteAsync(),
            "0 2 * * *"); // diariamente às 02:00 UTC
    }
}

/// <summary>
/// Filtro de autorização para o dashboard Hangfire.
/// Requer autenticação e role Admin.
/// </summary>
internal sealed class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
