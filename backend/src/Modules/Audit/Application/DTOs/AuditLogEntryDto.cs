namespace IMS.Modular.Modules.Audit.Application.DTOs;

/// <summary>
/// US-083: DTO for audit log entries returned by the API.
/// </summary>
public record AuditLogEntryDto(
    Guid Id,
    Guid? UserId,
    string? TenantId,
    string Action,
    string? EntityType,
    string? EntityId,
    string? OldValue,
    string? NewValue,
    string? IpAddress,
    DateTime Timestamp);
