namespace IMS.Modular.Modules.Webhooks.Application.DTOs;

/// <summary>US-069: DTO de retorno para registros de webhook.</summary>
public record WebhookRegistrationDto(
    Guid Id,
    string Url,
    List<string> Events,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>US-069: Request para criar webhook.</summary>
public record CreateWebhookRequest(
    string Url,
    List<string> Events);

/// <summary>US-069: Mensagem publicada na fila RabbitMQ para entrega.</summary>
public record WebhookDeliveryMessage(
    Guid WebhookRegistrationId,
    string Url,
    string Secret,
    string EventName,
    string Payload,
    string Signature);
