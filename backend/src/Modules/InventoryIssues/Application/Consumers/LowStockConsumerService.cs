using IMS.Modular.Modules.InventoryIssues.Application.Commands;
using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.InventoryIssues.Application.Consumers;

/// <summary>
/// US-047: BackgroundService that consumes LowStockAlert events from RabbitMQ
/// and creates an InventoryIssue automatically, preventing duplicates.
/// Subscribes to queue: ims.inventory.low_stock_alert (routing key: inventory.low_stock_alert)
/// </summary>
public sealed class LowStockConsumerService(
    IMessageBus messageBus,
    IServiceScopeFactory scopeFactory,
    ILogger<LowStockConsumerService> logger) : BackgroundService
{
    // Message contract matching the payload published by LowStockAlertOutboxHandler
    private sealed record LowStockAlertMessage(
        Guid ProductId,
        string SKU,
        string Name,
        int CurrentStock,
        int MinimumStockLevel,
        DateTime OccurredOn);

    // System reporter ID — use a well-known sentinel GUID for automated issues
    private static readonly Guid SystemReporterId = new("00000000-0000-0000-0000-000000000001");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[LowStockConsumer] Starting consumer for queue 'ims.inventory.low_stock_alert'");

        await messageBus.SubscribeAsync<LowStockAlertMessage>(
            queueName: "ims.inventory.low_stock_alert",
            handler: HandleAsync,
            cancellationToken: stoppingToken,
            exchange: "ims.inventory",
            bindingKey: "inventory.low_stock_alert");
    }

    private async Task HandleAsync(LowStockAlertMessage msg, CancellationToken ct)
    {
        logger.LogInformation("[LowStockConsumer] Received LowStockAlert for Product={ProductId} SKU={SKU} Stock={Current}/{Min}",
            msg.ProductId, msg.SKU, msg.CurrentStock, msg.MinimumStockLevel);

        using var scope = scopeFactory.CreateScope();

        // ── Duplicate guard ──────────────────────────────────────────────
        var db = scope.ServiceProvider.GetRequiredService<InventoryIssuesDbContext>();
        var alreadyOpen = await db.InventoryIssues.AnyAsync(
            i => i.ProductId == msg.ProductId
              && i.Type == InventoryIssueType.Shortage
              && (i.Status == InventoryIssueStatus.Open || i.Status == InventoryIssueStatus.InProgress),
            ct);

        if (alreadyOpen)
        {
            logger.LogDebug("[LowStockConsumer] Open shortage issue already exists for Product={ProductId}. Skipping.", msg.ProductId);
            return;
        }

        // ── Create InventoryIssue via MediatR ────────────────────────────
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var cmd = new CreateInventoryIssueCommand(
            Title: $"[ALERTA] Estoque baixo — {msg.Name} ({msg.SKU})",
            Description: $"O produto '{msg.Name}' (SKU: {msg.SKU}) está com estoque abaixo do mínimo. " +
                         $"Estoque atual: {msg.CurrentStock} un. | Estoque mínimo: {msg.MinimumStockLevel} un. " +
                         $"Evento gerado em: {msg.OccurredOn:u}",
            Type: InventoryIssueType.Shortage,
            Priority: msg.CurrentStock == 0 ? InventoryIssuePriority.Critical : InventoryIssuePriority.High,
            ReporterId: SystemReporterId,
            ProductId: msg.ProductId,
            LocationId: null,
            AffectedQuantity: msg.MinimumStockLevel - msg.CurrentStock,
            EstimatedLoss: null,
            DueDate: DateTime.UtcNow.AddDays(1));

        var issueId = await mediator.Send(cmd, ct);
        logger.LogInformation("[LowStockConsumer] InventoryIssue created: {IssueId} for Product={ProductId}", issueId, msg.ProductId);
    }
}
