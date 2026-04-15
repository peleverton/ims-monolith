namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Serviço para persistir mensagens no Outbox antes da publicação no RabbitMQ.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Persiste uma mensagem no Outbox. Deve ser chamado dentro da mesma transação
    /// em que o estado de domínio é salvo.
    /// </summary>
    Task SaveAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
        where T : class;
}
