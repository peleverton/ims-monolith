using IMS.Issues.Service.Infrastructure;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace IMS.Issues.Service.Messaging;

/// <summary>
/// US-079: Consumes integration events from the IMS Monolith published on RabbitMQ.
/// Listens for issue-related events (e.g., from monolith for cross-service sync).
/// </summary>
public class IssuesEventConsumer(
    IOptions<RabbitMqOptions> options,
    ILogger<IssuesEventConsumer> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var opt = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = opt.Host,
                Port = opt.Port,
                UserName = opt.Username,
                Password = opt.Password
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            var queueDeclare = await _channel.QueueDeclareAsync(
                queue: "ims.issues.service.inbox",
                durable: true, exclusive: false, autoDelete: false,
                cancellationToken: stoppingToken);

            // Bind to monolith-originated events
            await _channel.QueueBindAsync(queueDeclare.QueueName, opt.Exchange, "issues.#", cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 10, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                logger.LogInformation("[IssuesEventConsumer] Received event on {RoutingKey}: {Body}",
                    ea.RoutingKey, body);

                // Future: process cross-service events (e.g., user deactivated → unassign issues)
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };

            await _channel.BasicConsumeAsync(queueDeclare.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            logger.LogInformation("[IssuesEventConsumer] Listening on queue {Queue}", queueDeclare.QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[IssuesEventConsumer] RabbitMQ not available — consumer disabled");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
