using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace IMS.Modular.Shared.Messaging;

/// <summary>
/// US-023: Implementação do IMessageBus usando RabbitMQ.Client v7 (async API).
/// Gerencia uma única conexão persistente com retry automático via Polly v8.
/// </summary>
public sealed class RabbitMqMessageBusService : IMessageBus, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageBusService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMqMessageBusService(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMessageBusService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _options.RetryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "[RabbitMQ] Retry {Attempt} after {Delay}s", attempt, delay.TotalSeconds));
    }

    // ----------------------------------------------------------------
    // IMessageBus
    // ----------------------------------------------------------------

    /// <inheritdoc/>
    public async Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T message,
        CancellationToken cancellationToken = default) where T : class
    {
        // US-049: span so the full trace chain (handler → outbox → rabbit) is visible in Jaeger
        using var activity = OpenTelemetryExtensions.ActivitySource.StartActivity("RabbitMQ.Publish");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", exchange);
        activity?.SetTag("messaging.routing_key", routingKey);
        activity?.SetTag("messaging.message_type", typeof(T).Name);

        var connection = await GetConnectionAsync(cancellationToken);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug("[RabbitMQ] Published {MessageType} → {Exchange}/{RoutingKey}",
                typeof(T).Name, exchange, routingKey);
            activity?.SetTag("messaging.success", true);
        });
    }

    /// <inheritdoc/>
    public async Task SubscribeAsync<T>(
        string queueName,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default,
        string? exchange = null,
        string? bindingKey = null) where T : class
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Bind queue to exchange when provided (topic routing)
        if (!string.IsNullOrEmpty(exchange) && !string.IsNullOrEmpty(bindingKey))
        {
            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: exchange,
                routingKey: bindingKey,
                cancellationToken: cancellationToken);

            _logger.LogInformation("[RabbitMQ] Queue '{Queue}' bound to exchange '{Exchange}' with key '{Key}'",
                queueName, exchange, bindingKey);
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(ea.Body.Span, JsonOptions);
                if (message is not null)
                    await handler(message, cancellationToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RabbitMQ] Error processing message from {Queue}", queueName);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("[RabbitMQ] Subscribed to queue '{Queue}'", queueName);
    }

    // ----------------------------------------------------------------
    // Connection management
    // ----------------------------------------------------------------

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("[RabbitMQ] Connecting to {Host}:{Port}...", _options.Host, _options.Port);

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = await _retryPolicy.ExecuteAsync(
                async () => await factory.CreateConnectionAsync(clientProvidedName: "ims-monolith", cancellationToken));

            _logger.LogInformation("[RabbitMQ] Connected to {Host}:{Port}", _options.Host, _options.Port);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    // ----------------------------------------------------------------
    // Disposal
    // ----------------------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();
    }

    // ----------------------------------------------------------------
    // JSON options (shared)
    // ----------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
