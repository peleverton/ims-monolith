using IMS.Modular.Modules.Notifications.Application;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IMS.Modular.Modules.Notifications.Infrastructure;

/// <summary>
/// Placeholder implementation of IMessageBusService.
/// Replace with full RabbitMQ.Client v7.x async implementation in Phase 6.
/// </summary>
public class MessageBusService(ILogger<MessageBusService> logger) : IMessageBusService
{
    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        logger.LogInformation(
            "[MessageBus] Published to routing key '{RoutingKey}': {Payload}",
            routingKey, json);
        return Task.CompletedTask;
    }

    public Task PublishToExchangeAsync<T>(
        string exchange,
        string routingKey,
        T message,
        CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        logger.LogInformation(
            "[MessageBus] Published to exchange '{Exchange}' / key '{RoutingKey}': {Payload}",
            exchange, routingKey, json);
        return Task.CompletedTask;
    }
}
