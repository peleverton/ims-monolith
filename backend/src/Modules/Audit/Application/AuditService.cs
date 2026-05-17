using IMS.Modular.Modules.Audit.Domain;
using IMS.Modular.Modules.Audit.Infrastructure;

namespace IMS.Modular.Modules.Audit.Application;

/// <summary>
/// US-083: Persists audit log entries to the AuditLogs table.
/// </summary>
public sealed class AuditService(AuditDbContext db) : IAuditService
{
    public async Task LogAsync(
        string action,
        Guid? userId = null,
        string? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry(action, userId, tenantId, entityType, entityId, oldValue, newValue, ipAddress);
        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }
}
