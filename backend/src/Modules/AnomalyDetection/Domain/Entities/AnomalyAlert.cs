using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.AnomalyDetection.Domain.Entities;

/// <summary>
/// Alerta de anomalia detectada em movimentações de estoque.
/// </summary>
public class AnomalyAlert : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string? UserName { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? LocationName { get; private set; }

    public AnomalyType Type { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; } = AlertStatus.Open;

    public string Description { get; private set; } = null!;
    public int OccurrenceCount { get; private set; }
    public string WindowDescription { get; private set; } = null!;

    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public string? Resolution { get; private set; }

    private AnomalyAlert() { }

    public AnomalyAlert(
        string userId,
        string? userName,
        Guid? locationId,
        string? locationName,
        AnomalyType type,
        AlertSeverity severity,
        string description,
        int occurrenceCount,
        string windowDescription)
    {
        UserId = userId;
        UserName = userName;
        LocationId = locationId;
        LocationName = locationName;
        Type = type;
        Severity = severity;
        Description = description;
        OccurrenceCount = occurrenceCount;
        WindowDescription = windowDescription;
    }

    public void Acknowledge(string acknowledgedBy, string? resolution = null)
    {
        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = acknowledgedBy;
        Resolution = resolution;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Dismiss(string dismissedBy, string reason)
    {
        Status = AlertStatus.Dismissed;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = dismissedBy;
        Resolution = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Escalate()
    {
        Status = AlertStatus.Escalated;
        Severity = AlertSeverity.Critical;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AnomalyType
{
    ExcessiveAdjustments,
    ExcessiveLosses,
    RapidFireMovements,
    UnusualHoursActivity,
    HighValueLoss
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum AlertStatus
{
    Open,
    Acknowledged,
    Escalated,
    Dismissed
}
