using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Issues.Application.Consumers;

/// <summary>
/// US-048: BackgroundService that consumes IssueCreated events from RabbitMQ
/// and broadcasts a real-time notification to all connected clients via SignalR.
/// Subscribes to queue: ims.issues.created (routing key: issue.created)
/// </summary>
public sealed class IssueCreatedConsumerService(
    IMessageBus messageBus,
    IHubContext<NotificationsHub> hub,
    ILogger<IssueCreatedConsumerService> logger) : BackgroundService
{
    // Message contract matching the payload published by IssueCreatedOutboxHandler
    private sealed record IssueCreatedMessage(
        Guid IssueId,
        string Title,
        string Priority,
        Guid ReporterId,
        DateTime OccurredOn);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[IssueCreatedConsumer] Starting consumer for queue 'ims.issues.created'");

        await messageBus.SubscribeAsync<IssueCreatedMessage>(
            queueName: "ims.issues.created",
            handler: HandleAsync,
            cancellationToken: stoppingToken,
            exchange: "ims.issues",
            bindingKey: "issue.created");
    }

    private async Task HandleAsync(IssueCreatedMessage msg, CancellationToken ct)
    {
        logger.LogInformation("[IssueCreatedConsumer] Broadcasting NewIssue event for Issue={IssueId} Title='{Title}'",
            msg.IssueId, msg.Title);

        var payload = new
        {
            issueId = msg.IssueId,
            title = msg.Title,
            priority = msg.Priority,
            reporterId = msg.ReporterId,
            occurredOn = msg.OccurredOn
        };

        // Broadcast to all connected SignalR clients
        await hub.Clients.All.SendAsync("NewIssue", payload, ct);

        logger.LogInformation("[IssueCreatedConsumer] SignalR broadcast sent for Issue={IssueId}", msg.IssueId);
    }
}
