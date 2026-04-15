namespace IMS.Modular.Shared.Outbox;

/// <summary>
/// US-023: Opções de configuração do OutboxProcessor.
/// Lidas a partir da seção "Outbox" do appsettings.json.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Intervalo de polling em segundos (padrão: 15s).</summary>
    public int PollingIntervalSeconds { get; init; } = 15;

    /// <summary>Número de mensagens processadas por ciclo (padrão: 50).</summary>
    public int BatchSize { get; init; } = 50;

    /// <summary>Número máximo de tentativas antes de parar de tentar publicar (padrão: 5).</summary>
    public int MaxRetries { get; init; } = 5;
}
