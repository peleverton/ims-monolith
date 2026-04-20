namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// US-023: Abstração do barramento de mensagens.
/// Permite trocar a implementação (RabbitMQ, in-memory, etc.) sem alterar os produtores.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publica uma mensagem em uma fila/tópico específico.
    /// </summary>
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Registra um consumer assíncrono para uma fila.
    /// Quando <paramref name="exchange"/> e <paramref name="bindingKey"/> são fornecidos,
    /// a fila é automaticamente vinculada ao exchange (topic binding).
    /// </summary>
    Task SubscribeAsync<T>(
        string queueName,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default,
        string? exchange = null,
        string? bindingKey = null)
        where T : class;
}
