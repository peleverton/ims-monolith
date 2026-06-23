using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;

namespace IMS.Modular.Modules.AnomalyDetection.Application.DTOs;

public record AnomalyAlertDto(
    Guid Id,
    string UserId,
    string? UserName,
    Guid? LocationId,
    string? LocationName,
    AnomalyType Type,
    AlertSeverity Severity,
    AlertStatus Status,
    string Description,
    int OccurrenceCount,
    string WindowDescription,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt,
    string? AcknowledgedBy,
    string? Resolution);

public record AcknowledgeAlertRequest(string? Resolution);
public record DismissAlertRequest(string Reason);

public record AnomalyStatsSummaryDto(
    int TotalOpen,
    int TotalAcknowledged,
    int TotalEscalated,
    int TotalDismissed,
    int CriticalOpen,
    int HighOpen);
