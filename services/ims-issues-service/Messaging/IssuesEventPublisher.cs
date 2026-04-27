using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IMS.Issues.Service.Messaging;

/// <summary>
/// US-079: Publishes integration events from the Issues microservice to RabbitMQ.
/// Events consumed by the IMS Monolith for eventual consistency.
/// Initialization is lazy and fully async — no blocking calls on construction.
/// </summary>
public class IssuesEventPublisher(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly RabbitMqOptions _opt = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _unavailable;

    private async Task EnsureInitializedAsync()
    {
        if (_channel is not null || _unavailable) return;

        await _initLock.WaitAsync();
        try
        {
            if (_channel is not null || _unavailable) return;

            var factory = new ConnectionFactory
            {
                HostName = _opt.Host,
                Port = _opt.Port,
                UserName = _opt.Username,
                Password = _opt.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true);
        }
        catch
        {
            // Gracefully degrade if RabbitMQ is not available
            _unavailable = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync(string routingKey, object payload)
    {
        await EnsureInitializedAsync();

        if (_channel is null) return;

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(_opt.Exchange, routingKey, true, props, body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _initLock.Dispose();
    }
}
