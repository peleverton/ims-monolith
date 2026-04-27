using IMS.Modular.Modules.Webhooks.Application;
using IMS.Modular.Modules.Webhooks.Application.DTOs;
using IMS.Modular.Modules.Webhooks.Domain;
using IMS.Modular.Modules.Webhooks.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.Webhooks.Api;

/// <summary>
/// US-069: Endpoints de gerenciamento de webhooks.
///
/// POST   /api/webhooks        — registrar novo endpoint
/// GET    /api/webhooks        — listar registros do usuário autenticado
/// DELETE /api/webhooks/{id}   — remover (soft-delete / deactivate)
/// GET    /api/webhooks/events — listar eventos suportados
/// </summary>
public static class WebhooksModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .RequireAuthorization();

        // GET /api/webhooks/events — lista eventos suportados (não precisa de auth forte)
        group.MapGet("/events", () =>
            Results.Ok(WebhookEventNames.All))
            .WithName("GetWebhookEvents")
            .AllowAnonymous();

        // GET /api/webhooks
        group.MapGet("/", async (
            HttpContext http,
            IWebhookRepository repo,
            CancellationToken ct) =>
        {
            var ownerId = GetUserId(http);
            if (ownerId == Guid.Empty) return Results.Unauthorized();

            var list = await repo.GetByOwnerAsync(ownerId, ct);
            var dtos = list.Select(ToDto).ToList();
            return Results.Ok(dtos);
        }).WithName("GetWebhooks");

        // POST /api/webhooks
        group.MapPost("/", async (
            [FromBody] CreateWebhookRequest request,
            HttpContext http,
            IWebhookRepository repo,
            CancellationToken ct) =>
        {
            var ownerId = GetUserId(http);
            if (ownerId == Guid.Empty) return Results.Unauthorized();

            // Validate URL
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
                return Results.BadRequest(new { error = "URL inválida. Use http ou https." });

            // Validate events
            var invalid = request.Events.Except(WebhookEventNames.All, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalid.Count > 0)
                return Results.BadRequest(new { error = $"Eventos inválidos: {string.Join(", ", invalid)}" });

            // Generate secret
            var secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var registration = new WebhookRegistration(ownerId, request.Url, secret, request.Events);
            await repo.AddAsync(registration, ct);

            return Results.Created($"/api/webhooks/{registration.Id}", ToDto(registration));
        }).WithName("CreateWebhook");

        // DELETE /api/webhooks/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            IWebhookRepository repo,
            CancellationToken ct) =>
        {
            var ownerId = GetUserId(http);
            if (ownerId == Guid.Empty) return Results.Unauthorized();

            var registration = await repo.GetByIdAsync(id, ct);
            if (registration is null) return Results.NotFound();
            if (registration.OwnerId != ownerId) return Results.Forbid();

            registration.Deactivate();
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("DeleteWebhook");

        return endpoints;
    }

    private static Guid GetUserId(HttpContext http)
    {
        var claim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? http.User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static WebhookRegistrationDto ToDto(WebhookRegistration r) =>
        new(r.Id, r.Url, r.Events, r.IsActive, r.CreatedAt);
}
