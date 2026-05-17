using IMS.Modular.Modules.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-083: Hangfire job that deletes AuditLog entries older than the configured retention period.
/// Configurable via appsettings["Audit:RetentionDays"] (default: 90).
/// </summary>
public sealed class AuditLogRetentionJob(
    AuditDbContext db,
    IConfiguration configuration,
    ILogger<AuditLogRetentionJob> logger)
{
    public async Task ExecuteAsync()
    {
        var retentionDays = configuration.GetValue("Audit:RetentionDays", 90);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        logger.LogInformation("[AuditRetention] Deleting audit entries older than {Cutoff} (retention: {Days} days)",
            cutoff, retentionDays);

        var deleted = await db.AuditLogs
            .Where(e => e.Timestamp < cutoff)
            .ExecuteDeleteAsync();

        logger.LogInformation("[AuditRetention] Deleted {Count} audit log entries", deleted);
    }
}
