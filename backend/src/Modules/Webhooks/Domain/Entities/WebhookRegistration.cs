namespace IMS.Modular.Modules.Webhooks.Domain.Entities;

/// <summary>
/// US-069: Registro de um endpoint externo que receberá eventos via webhook.
/// </summary>
public class WebhookRegistration
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OwnerId { get; private set; }
    public string Url { get; private set; } = default!;
    public string Secret { get; private set; } = default!;
    public List<string> Events { get; private set; } = [];
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private WebhookRegistration() { }

    public WebhookRegistration(Guid ownerId, string url, string secret, IEnumerable<string> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        OwnerId = ownerId;
        Url = url;
        Secret = secret;
        Events = events.ToList();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ListensTo(string eventName) =>
        IsActive && Events.Contains(eventName, StringComparer.OrdinalIgnoreCase);
}
