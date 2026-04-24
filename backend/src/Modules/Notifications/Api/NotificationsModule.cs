using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Application.Commands;
using IMS.Modular.Modules.Notifications.Application.DTOs;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.Notifications.Api;

/// <summary>
/// US-066: Endpoints de notificações.
/// GET  /api/notifications           — histórico paginado do usuário autenticado
/// POST /api/notifications/{id}/read — marcar como lida
/// </summary>
public class NotificationsModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        // GET /api/notifications
        group.MapGet("/", async (
            HttpContext http,
            INotificationRepository repo,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(http);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var items = await repo.GetByUserAsync(userId, pageNumber, pageSize, ct);
            var unread = await repo.GetUnreadCountAsync(userId, ct);

            var dto = new NotificationPagedDto(
                Items: items.Select(n => new NotificationDto(
                    n.Id, n.Type, n.Title, n.Body, n.SentAt, n.IsRead, n.ReadAt)).ToList(),
                TotalUnread: unread,
                PageNumber: pageNumber,
                PageSize: pageSize);

            return Results.Ok(dto);
        });

        // POST /api/notifications/{id}/read
        group.MapPost("/{id:guid}/read", async (
            Guid id,
            HttpContext http,
            INotificationRepository repo,
            CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var notification = await repo.GetByIdAsync(id, ct);
            if (notification is null) return Results.NotFound();
            if (notification.UserId != userId) return Results.Forbid();

            notification.MarkAsRead();
            await repo.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return endpoints;
    }

    private static Guid GetUserId(HttpContext http)
    {
        var claim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? http.User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
