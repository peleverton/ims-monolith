using System.Text.Json;

namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Implementação do IOutboxService usando OutboxDbContext.
/// </summary>
public sealed class OutboxService(OutboxDbContext dbContext) : IOutboxService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc/>
    public async Task SaveAsync<T>(
        string exchange,
        string routingKey,
        T message,
        CancellationToken cancellationToken = default) where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
            Exchange = exchange,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(message, JsonOptions)
        };

        await dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
