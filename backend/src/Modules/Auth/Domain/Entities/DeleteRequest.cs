namespace IMS.Modular.Modules.Auth.Domain.Entities;

/// <summary>
/// US-089: LGPD/GDPR — tracks a user's right-to-delete request.
/// Soft-delete is applied immediately; hard-delete fires 30 days later via Hangfire.
/// </summary>
public class DeleteRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ScheduledHardDeleteAt { get; private set; }
    public DeleteRequestStatus Status { get; private set; } = DeleteRequestStatus.Pending;
    public Guid? CancelledByAdminId { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? ExecutedAt { get; private set; }

    private DeleteRequest() { }

    public static DeleteRequest Create(Guid userId, int hardDeleteAfterDays = 30)
        => new()
        {
            UserId = userId,
            ScheduledHardDeleteAt = DateTime.UtcNow.AddDays(hardDeleteAfterDays)
        };

    public void Cancel(Guid adminId)
    {
        if (Status != DeleteRequestStatus.Pending)
            throw new InvalidOperationException("Only pending delete requests can be cancelled.");

        Status = DeleteRequestStatus.Cancelled;
        CancelledByAdminId = adminId;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkExecuted()
    {
        Status = DeleteRequestStatus.Executed;
        ExecutedAt = DateTime.UtcNow;
    }
}

public enum DeleteRequestStatus
{
    Pending = 0,
    Cancelled = 1,
    Executed = 2
}
