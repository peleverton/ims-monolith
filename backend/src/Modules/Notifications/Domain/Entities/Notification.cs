namespace IMS.Modular.Modules.Notifications.Domain.Entities;

/// <summary>
/// US-066: Entidade de notificação persistida.
/// </summary>
public class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; private set; }
    public bool IsRead => ReadAt.HasValue;

    private Notification() { }

    public Notification(Guid userId, string type, string title, string body)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
            ReadAt = DateTime.UtcNow;
    }
}
