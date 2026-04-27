using IMS.Modular.Modules.Webhooks.Application.DTOs;
using IMS.Modular.Modules.Webhooks.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.Webhooks.Application;

/// <summary>
/// US-069: Consumer da fila "webhooks.delivery".
/// Faz HTTP POST ao endpoint externo, assina o payload e registra o resultado.
/// Retry: 3 tentativas com backoff exponencial (5s → 25s → 125s).
/// </summary>
public sealed class WebhookDeliveryConsumer(
    IMessageBus bus,
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookDeliveryConsumer> logger)
    : BackgroundService
{
    private static readonly TimeSpan[] Backoffs =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(125)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[WebhookConsumer] Starting subscription to webhooks.delivery");

        await bus.SubscribeAsync<WebhookDeliveryMessage>(
            queueName:  "webhooks.delivery",
            exchange:   "ims.webhooks",
            bindingKey: "webhooks.delivery",
            handler:    (msg, ct) => DeliverAsync(msg, ct),
            cancellationToken: stoppingToken);
    }

    private async Task DeliverAsync(WebhookDeliveryMessage msg, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("webhook");
        var delivery = new WebhookDelivery(msg.WebhookRegistrationId, msg.EventName, msg.Payload);

        bool success = false;
        int? statusCode = null;
        string? error = null;

        for (int attempt = 0; attempt <= Backoffs.Length; attempt++)
        {
            if (attempt > 0)
            {
                var delay = Backoffs[Math.Min(attempt - 1, Backoffs.Length - 1)];
                logger.LogWarning("[WebhookConsumer] Retry {Attempt} for {Url} in {Delay}s", attempt, msg.Url, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, msg.Url);
                request.Content = new StringContent(msg.Payload, System.Text.Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("X-IMS-Signature", msg.Signature);
                request.Headers.TryAddWithoutValidation("X-IMS-Event", msg.EventName);

                using var response = await http.SendAsync(request, ct);
                statusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                    logger.LogInformation("[WebhookConsumer] Delivered {Event} → {Url} [{Status}]",
                        msg.EventName, msg.Url, statusCode);
                    break;
                }

                error = $"HTTP {statusCode}";
                logger.LogWarning("[WebhookConsumer] Non-success {Status} for {Url}", statusCode, msg.Url);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                logger.LogWarning(ex, "[WebhookConsumer] Exception delivering to {Url}", msg.Url);
            }
        }

        delivery.RecordAttempt(success, statusCode, error);

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWebhookRepository>();
        await repo.SaveDeliveryAsync(delivery, ct);
    }
}
