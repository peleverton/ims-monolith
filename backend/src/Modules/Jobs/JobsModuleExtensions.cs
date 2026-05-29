using Hangfire;
using Hangfire.Dashboard;
using Hangfire.InMemory;
using Hangfire.PostgreSql;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.MultiTenancy;
using IMS.Modular.Shared.MultiTenancy.TenantManagement;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Background Jobs com Hangfire.
/// - Development: storage InMemory (zero infra)
/// - Production: storage PostgreSQL (persistência, retry automático)
/// US-086: Tenant-aware dashboard filter and global job filter added.
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
        services.AddScoped<GdprHardDeleteJob>();
        services.AddScoped<AuditLogRetentionJob>();
        // US-088: Meilisearch reindex job
        services.AddScoped<MeilisearchReindexJob>();
        // US-092: Tenant provisioning job (fire-and-forget, queued during self-service signup)
        services.AddScoped<TenantProvisioningJob>();

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

        // US-086: Register tenant job filter so all enqueued jobs carry their tenant tag
        services.AddSingleton<TenantJobFilter>();

        return services;
    }

    public static void UseJobsModule(this WebApplication app)
    {
        // US-086: Dashboard with tenant-aware auth filter (resolves ITenantService per-request)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new TenantAwareHangfireDashboardFilter()],
            AppPath = "/",
            DashboardTitle = "IMS — Background Jobs",
        });

        // US-086: Register tenant job filter globally
        var tenantFilter = app.Services.GetRequiredService<TenantJobFilter>();
        GlobalJobFilters.Filters.Add(tenantFilter);

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

        manager.AddOrUpdate<GdprHardDeleteJob>(
            "gdpr-hard-delete",
            job => job.ExecuteAsync(),
            "0 3 * * *"); // diariamente às 03:00 UTC

        // US-083: Audit log retention — daily at 04:00 UTC
        manager.AddOrUpdate<AuditLogRetentionJob>(
            "audit-log-retention",
            job => job.ExecuteAsync(),
            "0 4 * * *");

        // US-088: Meilisearch drift detection — weekly on Sunday at 05:00 UTC
        manager.AddOrUpdate<MeilisearchReindexJob>(
            "meilisearch-reindex",
            job => job.ExecuteAsync(false),
            "0 5 * * 0");
    }
}
