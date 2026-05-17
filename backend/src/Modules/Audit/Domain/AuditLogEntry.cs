namespace IMS.Modular.Modules.Audit.Domain;

/// <summary>
/// US-083: Persistent audit log entry for compliance and forensics.
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public string? TenantId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private AuditLogEntry() { }

    public AuditLogEntry(
        string action,
        Guid? userId = null,
        string? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null)
    {
        Action = action;
        UserId = userId;
        TenantId = tenantId;
        EntityType = entityType;
        EntityId = entityId;
        OldValue = oldValue;
        NewValue = newValue;
        IpAddress = ipAddress;
    }
}
