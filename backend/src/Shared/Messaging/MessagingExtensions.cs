using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Shared.Messaging;

/// <summary>
/// US-023: Extensões de DI para o barramento de mensagens RabbitMQ.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Registra o IMessageBus com a implementação RabbitMQ.
    /// Se "RabbitMQ:Host" não estiver configurado, registra uma implementação no-op
    /// que loga um aviso e ignora as mensagens — permite que a aplicação suba sem RabbitMQ
    /// em ambientes de CI ou desenvolvimento sem Docker.
    /// </summary>
    public static IServiceCollection AddImsMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(RabbitMqOptions.SectionName);
        var host = section["Host"];

        if (!string.IsNullOrWhiteSpace(host))
        {
            // RabbitMQ configurado → registra implementação real
            services.Configure<RabbitMqOptions>(section);
            services.AddSingleton<IMessageBus, RabbitMqMessageBusService>();
        }
        else
        {
            // Sem RabbitMQ configurado → implementação no-op para dev/CI sem Docker
            services.AddSingleton<IMessageBus, NullMessageBusService>();
        }

        return services;
    }
}

/// <summary>
/// Implementação no-op do IMessageBus.
/// Usada quando RabbitMQ não está configurado (dev sem Docker, CI, testes).
/// </summary>
internal sealed class NullMessageBusService(ILogger<NullMessageBusService> logger) : IMessageBus
{
    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogDebug("[MessageBus:Null] PublishAsync ignored — RabbitMQ not configured. Exchange={Exchange}, RoutingKey={RoutingKey}",
            exchange, routingKey);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string queueName, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogDebug("[MessageBus:Null] SubscribeAsync ignored — RabbitMQ not configured. Queue={Queue}", queueName);
        return Task.CompletedTask;
    }
}
