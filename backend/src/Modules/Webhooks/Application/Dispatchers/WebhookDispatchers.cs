using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Modules.Webhooks.Application.DTOs;
using IMS.Modular.Modules.Webhooks.Domain;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IMS.Modular.Modules.Webhooks.Application.Dispatchers;

/// <summary>
/// US-069: Escuta IssueCreatedEvent e enfileira deliveries para webhooks registrados.
/// </summary>
public sealed class IssueCreatedWebhookDispatcher(
    IWebhookRepository repo,
    IMessageBus bus,
    ILogger<IssueCreatedWebhookDispatcher> logger)
    : INotificationHandler<IssueCreatedEvent>
{
    public async Task Handle(IssueCreatedEvent notification, CancellationToken ct)
    {
        var registrations = await repo.GetActiveByEventAsync(WebhookEventNames.IssueCreated, ct);
        if (registrations.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            @event = WebhookEventNames.IssueCreated,
            timestamp = notification.OccurredOn,
            data = new
            {
                issueId   = notification.IssueId,
                title     = notification.Title,
                priority  = notification.Priority.ToString(),
                reporterId = notification.ReporterId,
            }
        });

        foreach (var reg in registrations)
        {
            var signature = WebhookSigner.Sign(reg.Secret, payload);
            var msg = new WebhookDeliveryMessage(reg.Id, reg.Url, reg.Secret, WebhookEventNames.IssueCreated, payload, signature);
            await bus.PublishAsync("ims.webhooks", "webhooks.delivery", msg, ct);
            logger.LogDebug("[Webhook] Enqueued {Event} → {Url}", WebhookEventNames.IssueCreated, reg.Url);
        }
    }
}

/// <summary>
/// US-069: Escuta IssueCompletedEvent (status Resolved/Closed) e enfileira deliveries.
/// </summary>
public sealed class IssueResolvedWebhookDispatcher(
    IWebhookRepository repo,
    IMessageBus bus,
    ILogger<IssueResolvedWebhookDispatcher> logger)
    : INotificationHandler<IssueCompletedEvent>
{
    public async Task Handle(IssueCompletedEvent notification, CancellationToken ct)
    {
        var registrations = await repo.GetActiveByEventAsync(WebhookEventNames.IssueResolved, ct);
        if (registrations.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            @event = WebhookEventNames.IssueResolved,
            timestamp = notification.OccurredOn,
            data = new
            {
                issueId     = notification.IssueId,
                completedBy = notification.CompletedBy,
                duration    = notification.Duration.TotalMinutes,
            }
        });

        foreach (var reg in registrations)
        {
            var signature = WebhookSigner.Sign(reg.Secret, payload);
            var msg = new WebhookDeliveryMessage(reg.Id, reg.Url, reg.Secret, WebhookEventNames.IssueResolved, payload, signature);
            await bus.PublishAsync("ims.webhooks", "webhooks.delivery", msg, ct);
            logger.LogDebug("[Webhook] Enqueued {Event} → {Url}", WebhookEventNames.IssueResolved, reg.Url);
        }
    }
}
