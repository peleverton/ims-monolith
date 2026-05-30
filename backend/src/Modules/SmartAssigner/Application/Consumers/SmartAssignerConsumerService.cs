using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.SmartAssigner.Application.Chain;
using IMS.Modular.Modules.SmartAssigner.Domain.Models;
using IMS.Modular.Modules.SmartAssigner.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;

namespace IMS.Modular.Modules.SmartAssigner.Application.Consumers;

/// <summary>
/// BackgroundService that consumes IssueCreated events from RabbitMQ.
/// When a new issue is created without an assignee, the Smart Assigner
/// automatically determines the best candidate and assigns the issue.
/// </summary>
public sealed class SmartAssignerConsumerService(
    IMessageBus messageBus,
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationsHub> hub,
    ILogger<SmartAssignerConsumerService> logger) : BackgroundService
{
    private sealed record IssueCreatedMessage(
        Guid IssueId,
        string Title,
        string Priority,
        Guid ReporterId,
        DateTime OccurredOn);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[SmartAssigner] Starting consumer for queue 'ims.smart-assigner.issue-created'");

        await messageBus.SubscribeAsync<IssueCreatedMessage>(
            queueName: "ims.smart-assigner.issue-created",
            handler: HandleAsync,
            cancellationToken: stoppingToken,
            exchange: "ims.issues",
            bindingKey: "issue.created");
    }

    private async Task HandleAsync(IssueCreatedMessage msg, CancellationToken ct)
    {
        logger.LogInformation(
            "[SmartAssigner] Processing assignment for Issue={IssueId} Title='{Title}' Priority={Priority}",
            msg.IssueId, msg.Title, msg.Priority);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISmartAssignerRepository>();
        var chainExecutor = scope.ServiceProvider.GetRequiredService<AssignmentChainExecutor>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Build assignment context
        var candidates = await repository.GetCandidatesAsync(ct);
        var issueTags = await repository.GetIssueTagsAsync(msg.IssueId, ct);

        if (candidates.Count == 0)
        {
            logger.LogWarning("[SmartAssigner] No candidates available for Issue={IssueId}", msg.IssueId);
            return;
        }

        var context = new AssignmentContext
        {
            IssueId = msg.IssueId,
            IssueTitle = msg.Title,
            Priority = Enum.TryParse<IssuePriority>(msg.Priority, out var p) ? p : IssuePriority.Medium,
            RequiredTags = issueTags,
            Candidates = candidates
        };

        // Execute the chain of responsibility
        var result = await chainExecutor.ExecuteAsync(context, ct);

        if (result.SelectedAssigneeId is null)
        {
            logger.LogWarning(
                "[SmartAssigner] Could not assign Issue={IssueId}. Reason: {Reason}",
                msg.IssueId, result.Reason);
            return;
        }

        // Dispatch assignment command via MediatR
        var command = new IMS.Modular.Modules.Issues.Application.Commands.AssignIssueCommand(
            msg.IssueId,
            result.SelectedAssigneeId.Value,
            Guid.Empty); // System-assigned (no user context)

        var assignResult = await mediator.Send(command, ct);

        if (assignResult.IsSuccess)
        {
            logger.LogInformation(
                "[SmartAssigner] Issue={IssueId} auto-assigned to User={UserId}. Reason: {Reason}",
                msg.IssueId, result.SelectedAssigneeId, result.Reason);

            // Broadcast to SignalR for real-time dashboard (Epic 8)
            await hub.Clients.All.SendAsync("IssueAutoAssigned", new
            {
                issueId = msg.IssueId,
                issueTitle = msg.Title,
                priority = msg.Priority,
                assigneeId = result.SelectedAssigneeId,
                reason = result.Reason,
                occurredOn = DateTime.UtcNow
            }, ct);
        }
        else
        {
            logger.LogError(
                "[SmartAssigner] Failed to assign Issue={IssueId} to User={UserId}. Error: {Error}",
                msg.IssueId, result.SelectedAssigneeId, assignResult.Error);
        }
    }
}
