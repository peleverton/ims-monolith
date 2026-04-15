namespace IMS.Modular.Shared.Messaging;

/// <summary>
/// US-023: Opções de configuração para o RabbitMQ.
/// Lidas a partir da seção "RabbitMQ" do appsettings.json.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";

    /// <summary>Número de tentativas de retry ao conectar ou publicar.</summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>Prefixo usado para nomear exchanges e filas do projeto.</summary>
    public string ExchangePrefix { get; init; } = "ims";
}
