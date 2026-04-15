namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Mensagem persistida no Outbox antes de ser publicada no RabbitMQ.
/// Garante entrega pelo menos uma vez (at-least-once delivery) em caso de falha.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nome qualificado do tipo da mensagem (para desserialização).</summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>Exchange destino no RabbitMQ.</summary>
    public string Exchange { get; init; } = string.Empty;

    /// <summary>Routing key usada na publicação.</summary>
    public string RoutingKey { get; init; } = string.Empty;

    /// <summary>Payload serializado em JSON.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>Data/hora em que a mensagem foi criada e persistida.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Data/hora em que a mensagem foi publicada com sucesso. Null = pendente.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Número de tentativas de publicação.</summary>
    public int RetryCount { get; set; }

    /// <summary>Última mensagem de erro ao tentar publicar.</summary>
    public string? LastError { get; set; }
}
