namespace IMS.Modular.Modules.Audit.Application;

/// <summary>
/// US-083: Service to write audit log entries.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        Guid? userId = null,
        string? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}
