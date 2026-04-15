using IMS.Modular.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Background service que processa mensagens pendentes no Outbox e as publica no RabbitMQ.
/// Executa em polling a cada N segundos (configurável via OutboxOptions).
/// </summary>
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IMessageBus messageBus,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[Outbox] Processor started. Interval={IntervalSeconds}s", _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[Outbox] Unhandled error during processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("[Outbox] Processor stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        logger.LogDebug("[Outbox] Processing {Count} pending message(s)", pending.Count);

        foreach (var msg in pending)
        {
            try
            {
                // Deserializa usando o tipo original
                var type = Type.GetType(msg.MessageType);
                if (type is null)
                {
                    logger.LogWarning("[Outbox] Could not resolve type '{Type}' for message {Id}. Skipping.", msg.MessageType, msg.Id);
                    msg.RetryCount++;
                    msg.LastError = $"Type not found: {msg.MessageType}";
                    continue;
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize(msg.Payload, type);
                if (payload is null)
                {
                    msg.RetryCount++;
                    msg.LastError = "Deserialization returned null";
                    continue;
                }

                // Publica via IMessageBus (reflection para chamar método genérico)
                var publishMethod = typeof(IMessageBus)
                    .GetMethod(nameof(IMessageBus.PublishAsync))!
                    .MakeGenericMethod(type);

                await (Task)publishMethod.Invoke(messageBus, [msg.Exchange, msg.RoutingKey, payload, cancellationToken])!;

                msg.ProcessedAt = DateTime.UtcNow;
                logger.LogDebug("[Outbox] Message {Id} published successfully", msg.Id);
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.LastError = ex.Message;
                logger.LogWarning(ex, "[Outbox] Failed to publish message {Id} (attempt {Attempt})", msg.Id, msg.RetryCount);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
