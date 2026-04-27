namespace IMS.Modular.Modules.Webhooks.Domain.Entities;

/// <summary>
/// US-069: Registro de cada tentativa de entrega de um webhook.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WebhookRegistrationId { get; private set; }
    public string EventName { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public int Attempts { get; private set; }
    public bool Success { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; private set; }

    private WebhookDelivery() { }

    public WebhookDelivery(Guid webhookRegistrationId, string eventName, string payload)
    {
        WebhookRegistrationId = webhookRegistrationId;
        EventName = eventName;
        Payload = payload;
    }

    public void RecordAttempt(bool success, int? statusCode, string? error)
    {
        Attempts++;
        Success = success;
        ResponseStatusCode = statusCode;
        ErrorMessage = error;
        LastAttemptAt = DateTime.UtcNow;
    }
}
